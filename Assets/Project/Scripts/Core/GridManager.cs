using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;

/// <summary>
/// Manages the grid system for tower placement and pathfinding in the tower defense game.
/// Handles grid creation, cell validation, and visualization.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Width of the grid in cells")]
    [SerializeField] private int gridWidth = 10;
    
    [Tooltip("Height of the grid in cells")]
    [SerializeField] private int gridHeight = 10;
    
    [Tooltip("Size of each grid cell in world units")]
    [SerializeField] private float cellSize = 1f;
    
    [Header("Visualization")]
    [Tooltip("Visual representation of a grid cell")]
    [SerializeField] private GameObject cellPrefab;
    
    [Tooltip("Material for valid placement")]
    [SerializeField] private Material validPlacementMaterial;
    
    [Tooltip("Material for invalid placement")]
    [SerializeField] private Material invalidPlacementMaterial;
    
    [Tooltip("Material for cell visualization")]
    [SerializeField] private Material defaultCellMaterial;
    
    [Tooltip("Material for path visualization")]
    [SerializeField] private Material pathMaterial;
    
    [Header("Dependencies")]
    [Tooltip("Reference to the pathfinding manager")]
    [SerializeField] private PathfindingManager pathfindingManager;
    
    [Header("Optimization")]
    [Tooltip("Should cell visualizers be pooled?")]
    [SerializeField] private bool useCellPooling = true;
    
    [Tooltip("Maximum number of cells to instantiate when pooling")]
    [SerializeField] private int initialPoolSize = 100;
    
    [Tooltip("Should use spatial partitioning for cell lookup?")]
    [SerializeField] private bool useSpatialPartitioning = true;
    
    [Tooltip("View distance for cell culling (0 = no culling)")]
    [SerializeField] private float viewDistance = 20f;

    // The grid data structure that tracks cell occupancy and properties
    private GridCell[,] grid;
    
    // Dictionary to track cell visualizers
    private Dictionary<Vector2Int, GameObject> cellVisualizers = new Dictionary<Vector2Int, GameObject>();
    
    // Object pool for cell visualizers
    private GridObjectPool cellVisualizerPool;
    
    // Currently highlighted cell
    private Vector2Int? highlightedCell = null;
    
    // Reference to current path visualized
    private List<Vector2Int> currentPath = new List<Vector2Int>();

    // Flag to show/hide the grid
    private bool isGridVisible = false;

    private float pathHeight;
    
    // Material cache
    private Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
    
    // Spatial partitioning for cell lookup
    private GridSpatialPartitioning spatialPartitioning;
    
    // Camera reference for culling
    private Camera mainCamera;
    
    // Cached grid bounds for quick access
    private Bounds gridBounds;

    private void Awake()
    {
        // Initialize the grid
        InitializeGrid();
        
        // Initialize material cache
        InitializeMaterialCache();
        
        // Register with ServiceLocator
        ServiceLocator.Register<GridManager>(this);
    }

    private void Start()
    {
        // Find dependencies if not assigned in Inspector
        ResolveDependencies();
    
        // Cache main camera reference for culling
        mainCamera = Camera.main;
    
        // Initialize object pool FIRST - before anyone tries to use it
        if (useCellPooling)
        {
            cellVisualizerPool = new GridObjectPool(cellPrefab, transform, 
                                               Mathf.Min(initialPoolSize, gridWidth * gridHeight),
                                               gridWidth * gridHeight);
        }
    
        // Initialize spatial partitioning
        if (useSpatialPartitioning)
        {
            spatialPartitioning = new GridSpatialPartitioning(gridWidth, gridHeight);
        }
    
        // Calculate grid bounds
        gridBounds = new Bounds(
            new Vector3(gridWidth * cellSize / 2, 0, gridHeight * cellSize / 2),
            new Vector3(gridWidth * cellSize, 0.1f, gridHeight * cellSize));
    
        // Create cell visualizers
        CreateCellVisualizers();
    
        // Hide grid initially LAST - after all initialization is done
        SetGridVisibility(false);
    }
    
    private void OnDestroy()
    {
        // Unregister from ServiceLocator
        ServiceLocator.Unregister<GridManager>();
        
        // Clean up object pool if exists
        if (cellVisualizerPool != null)
        {
            cellVisualizerPool.Clear();
        }
        
        materialCache.Clear();
    }
    
    /// <summary>
    /// Initialize material cache to avoid creating new materials for each cell
    /// </summary>
    private void InitializeMaterialCache()
    {
        // Cache default material
        if (defaultCellMaterial != null)
        {
            materialCache["default"] = defaultCellMaterial;
        }
        
        // Cache valid placement material
        if (validPlacementMaterial != null)
        {
            materialCache["valid"] = validPlacementMaterial;
        }
        
        // Cache invalid placement material
        if (invalidPlacementMaterial != null)
        {
            materialCache["invalid"] = invalidPlacementMaterial;
        }
        
        // Cache path material
        if (pathMaterial != null)
        {
            materialCache["path"] = pathMaterial;
        }
    }
    
    /// <summary>
    /// Get a material from cache by key
    /// </summary>
    private Material GetMaterial(string key)
    {
        if (materialCache.TryGetValue(key, out Material material))
        {
            return material;
        }
        return null;
    }
    
    /// <summary>
    /// Find any required dependencies not assigned in Inspector
    /// </summary>
    private void ResolveDependencies()
    {
        if (pathfindingManager == null)
        {
            // Try to get from ServiceLocator first
            pathfindingManager = ServiceLocator.Get<PathfindingManager>(true);
            
            // Fallback to FindObjectOfType if not in ServiceLocator
            if (pathfindingManager == null)
            {
                pathfindingManager = FindObjectOfType<PathfindingManager>();
                
                if (pathfindingManager == null)
                {
                    Debug.LogWarning("GridManager: Could not find PathfindingManager. Pathfinding validation will be disabled.");
                }
            }
        }
    }

    /// <summary>
    /// Initializes the grid data structure
    /// </summary>
    private void InitializeGrid()
    {
        grid = new GridCell[gridWidth, gridHeight];
        
        // Initialize each cell in the grid
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = new GridCell(true);
            }
        }
        
        Debug.Log($"Grid initialized with dimensions: {gridWidth}x{gridHeight}");
    }

    /// <summary>
    /// Creates visual representations of grid cells for debugging and placement indication
    /// </summary>
    private void CreateCellVisualizers()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("Cell prefab not assigned to GridManager!");
            return;
        }

        // With object pooling, cells are created on-demand when needed
        if (!useCellPooling)
        {
            // Create a visualizer for each cell in the grid
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 position = GetWorldPosition(new Vector2Int(x, y));
                    GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
                    
                    // Set the cell's scale to match cell size
                    cell.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
                    
                    // Set the default material
                    Renderer renderer = cell.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = GetMaterial("default");
                    }
                    
                    cell.name = $"Cell_{x}_{y}";
                    cellVisualizers.Add(new Vector2Int(x, y), cell);
                }
            }
        }
    }

    /// <summary>
    /// Shows or hides the visualized grid with view frustum culling
    /// </summary>
    public void SetGridVisibility(bool isVisible)
    {
        isGridVisible = isVisible;
        
        if (useCellPooling)
        {
            // When using pooling, we only activate/deactivate cells when needed
            if (!isVisible)
            {
                // Deactivate all active cells and return them to the pool
                foreach (var visualizer in cellVisualizers.Values)
                {
                    cellVisualizerPool.Return(visualizer);
                }
                cellVisualizers.Clear();
            }
            else if (viewDistance > 0 && mainCamera != null)
            {
                // Only create visualizers for cells in view
                List<Vector2Int> visibleCells;
                
                if (useSpatialPartitioning && spatialPartitioning != null)
                {
                    visibleCells = spatialPartitioning.GetCellsInView(mainCamera, cellSize);
                }
                else
                {
                    // Fallback to showing all cells
                    visibleCells = new List<Vector2Int>();
                    for (int x = 0; x < gridWidth; x++)
                    {
                        for (int y = 0; y < gridHeight; y++)
                        {
                            visibleCells.Add(new Vector2Int(x, y));
                        }
                    }
                }
                
                // Create visualizers for visible cells
                foreach (Vector2Int pos in visibleCells)
                {
                    GetCellVisualizer(pos);
                }
            }
        }
        else
        {
            // Simply set active/inactive for each cell
            foreach (var visualizer in cellVisualizers.Values)
            {
                visualizer.SetActive(isVisible);
            }
        }
    }

    /// <summary>
    /// Toggles the grid visibility
    /// </summary>
    public void ToggleGridVisibility()
    {
        SetGridVisibility(!isGridVisible);
    }

    public void SetPathHeight(float height)
    {
        pathHeight = height;
        Debug.Log($"Path height set to {pathHeight}");
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition, float heightOverride = float.MinValue)
    {
        // If a specific height is provided, use it, otherwise use the stored path height
        float yValue = (heightOverride != float.MinValue) ? heightOverride : pathHeight;
    
        return new Vector3(
            gridPosition.x * cellSize + (cellSize / 2),
            yValue,
            gridPosition.y * cellSize + (cellSize / 2)
        );
    }

    /// <summary>
    /// Converts a world position to a grid position
    /// </summary>
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / cellSize);
        int y = Mathf.FloorToInt(worldPosition.z / cellSize);
        
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Checks if a position is within the grid boundaries
    /// </summary>
    public bool IsWithinGrid(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridWidth && 
               gridPosition.y >= 0 && gridPosition.y < gridHeight;
    }

    /// <summary>
    /// Checks if a tower can be placed at the specified grid position
    /// </summary>
    public bool CanPlaceTower(Vector2Int gridPosition)
    {
        // Check if position is within grid
        if (!IsWithinGrid(gridPosition))
        {
            return false;
        }
        
        // Check if cell is already occupied
        if (grid[gridPosition.x, gridPosition.y].IsOccupied)
        {
            return false;
        }
        
        // Check if placement would block the path
        if (pathfindingManager != null)
        {
            // Create a simulation of the grid with the tower placed
            GridCell[,] simulatedGrid = CreateSimulatedGrid(gridPosition);
            
            // Check if there's still a valid path with the tower placed
            bool pathExists = pathfindingManager.CheckPathExists(simulatedGrid);
            
            if (!pathExists)
            {
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Creates a simulated grid with a tower placed at the specified position
    /// </summary>
    private GridCell[,] CreateSimulatedGrid(Vector2Int towerPosition)
    {
        // Create a copy of the current grid
        GridCell[,] simulatedGrid = new GridCell[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell original = grid[x, y];
                simulatedGrid[x, y] = new GridCell(original.IsWalkable);
                simulatedGrid[x, y].IsOccupied = original.IsOccupied;
                simulatedGrid[x, y].OccupyingObject = original.OccupyingObject;
            }
        }
        
        // Place the tower in the simulated grid
        simulatedGrid[towerPosition.x, towerPosition.y].IsOccupied = true;
        simulatedGrid[towerPosition.x, towerPosition.y].IsWalkable = false;
        
        return simulatedGrid;
    }

    /// <summary>
    /// Places a tower at the specified grid position
    /// </summary>
    public bool PlaceTower(Vector2Int gridPosition, GameObject towerPrefab, out GameObject placedTower)
    {
        placedTower = null;
        
        if (!CanPlaceTower(gridPosition))
        {
            return false;
        }
        
        Vector3 worldPosition = GetWorldPosition(gridPosition);
        placedTower = Instantiate(towerPrefab, worldPosition, Quaternion.identity);
        
        // Update the grid data
        grid[gridPosition.x, gridPosition.y].IsOccupied = true;
        grid[gridPosition.x, gridPosition.y].IsWalkable = false;
        grid[gridPosition.x, gridPosition.y].OccupyingObject = placedTower;
        
        // Notify pathfinding that the grid has changed
        if (pathfindingManager != null)
        {
            pathfindingManager.RecalculatePath();
        }
        
        return true;
    }

    /// <summary>
    /// Removes a tower from the specified grid position
    /// </summary>
    public bool RemoveTower(Vector2Int gridPosition)
    {
        if (!IsWithinGrid(gridPosition) || !grid[gridPosition.x, gridPosition.y].IsOccupied)
        {
            return false;
        }
        
        // Destroy the tower game object
        if (grid[gridPosition.x, gridPosition.y].OccupyingObject != null)
        {
            Destroy(grid[gridPosition.x, gridPosition.y].OccupyingObject);
        }
        
        // Reset the grid data
        grid[gridPosition.x, gridPosition.y].IsOccupied = false;
        grid[gridPosition.x, gridPosition.y].IsWalkable = true;
        grid[gridPosition.x, gridPosition.y].OccupyingObject = null;
        
        // Notify pathfinding that the grid has changed
        if (pathfindingManager != null)
        {
            pathfindingManager.RecalculatePath();
        }
        
        return true;
    }

    /// <summary>
    /// Highlights a grid cell to indicate valid/invalid placement
    /// </summary>
    public void HighlightCell(Vector2Int gridPosition, bool isValid)
    {
        // Reset previous highlighted cell
        if (highlightedCell.HasValue && highlightedCell.Value != gridPosition)
        {
            ResetCellHighlight(highlightedCell.Value);
        }
        
        // Set new highlighted cell
        highlightedCell = gridPosition;
        
        if (!IsWithinGrid(gridPosition))
        {
            return;
        }
        
        // Get or create cell visualizer
        GameObject cellVisualizer = GetCellVisualizer(gridPosition);
        
        if (cellVisualizer != null)
        {
            // Set the appropriate material based on placement validity
            Renderer renderer = cellVisualizer.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = isValid ? GetMaterial("valid") : GetMaterial("invalid");
            }
            
            // Ensure the cell is visible
            cellVisualizer.SetActive(true);
        }
    }

    /// <summary>
    /// Resets the highlight on a cell
    /// </summary>
    public void ResetCellHighlight(Vector2Int gridPosition)
    {
        if (!IsWithinGrid(gridPosition))
        {
            return;
        }
        
        GameObject cellVisualizer = GetCellVisualizer(gridPosition);
        
        if (cellVisualizer != null)
        {
            if (isGridVisible)
            {
                // If the grid is visible, reset to default material
                Renderer renderer = cellVisualizer.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = GetMaterial("default");
                }
            }
            else if (useCellPooling)
            {
                // If using pooling and grid not visible, return to pool
                cellVisualizerPool.Return(cellVisualizer);
                cellVisualizers.Remove(gridPosition);
            }
            else
            {
                // If not using pooling, just hide it
                cellVisualizer.SetActive(false);
            }
        }
        
        // Clear highlighted cell reference if it's the current one
        if (highlightedCell.HasValue && highlightedCell.Value == gridPosition)
        {
            highlightedCell = null;
        }
    }

        /// <summary>
        /// Visualizes a path through the grid
        /// </summary>
        public void VisualizePath(List<Vector2Int> path)
        {
            if (path == null)
            {
                Debug.LogWarning("GridManager: Cannot visualize null path");
                return;
            }
    
            // Reset previous path visualization
            foreach (var position in currentPath)
            {
                ResetCellHighlight(position);
            }
    
            currentPath = new List<Vector2Int>(path);
    
            // Highlight the new path
            foreach (var position in path)
            {
                if (IsWithinGrid(position))
                {
                    GameObject cellVisualizer = GetCellVisualizer(position);
            
                    if (cellVisualizer != null)
                    {
                        // Set path material
                        Renderer renderer = cellVisualizer.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            Material material = GetMaterial("path");
                            if (material != null)
                            {
                                renderer.material = material;
                            }
                        }
                
                        // Ensure the cell is visible
                        cellVisualizer.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or creates a cell visualizer for the specified grid position using object pooling
        /// </summary>
        private GameObject GetCellVisualizer(Vector2Int gridPosition)
        {
            if (!IsWithinGrid(gridPosition))
            {
                return null;
            }
    
            if (useCellPooling)
            {
                // If we already have a visualizer for this position, return it
                if (cellVisualizers.TryGetValue(gridPosition, out GameObject existingVisualizer))
                {
                    return existingVisualizer;
                }
        
                // Check if cell pool is initialized
                if (cellVisualizerPool == null)
                {
                    Debug.LogWarning("GridManager: Cell visualizer pool is null!");
                    return null;
                }
        
                // Otherwise, get one from the pool
                GameObject cellVisualizer = cellVisualizerPool.Get();
                if (cellVisualizer == null) return null;
        
                // Position the visualizer
                Vector3 worldPosition = GetWorldPosition(gridPosition);
                cellVisualizer.transform.position = worldPosition;
        
                // Ensure proper scale
                cellVisualizer.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
        
                // Reset material
                Renderer renderer = cellVisualizer.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = GetMaterial("default");
                    if (material != null)
                    {
                        renderer.material = material;
                    }
                }
        
                cellVisualizer.name = $"Cell_{gridPosition.x}_{gridPosition.y}";
                cellVisualizers.Add(gridPosition, cellVisualizer);
        
                return cellVisualizer;
            }
            else
            {
                // If not using pooling, just return the existing visualizer
                if (cellVisualizers.TryGetValue(gridPosition, out GameObject visualizer))
                {
                    return visualizer;
                }
            }
    
            return null;
        }

    /// <summary>
    /// Gets the grid cell at the specified position
    /// </summary>
    public GridCell GetCell(Vector2Int gridPosition)
    {
        if (!IsWithinGrid(gridPosition))
        {
            return new GridCell(false);
        }
        
        return grid[gridPosition.x, gridPosition.y];
    }

    /// <summary>
    /// Gets the dimensions of the grid
    /// </summary>
    public Vector2Int GetGridDimensions()
    {
        return new Vector2Int(gridWidth, gridHeight);
    }

    /// <summary>
    /// Gets the cell size
    /// </summary>
    public float GetCellSize()
    {
        return cellSize;
    }

    /// <summary>
    /// Sets specific cells as unwalkable (for level design)
    /// </summary>
    public void SetUnwalkableCells(List<Vector2Int> unwalkableCells)
    {
        foreach (var position in unwalkableCells)
        {
            if (IsWithinGrid(position))
            {
                grid[position.x, position.y].IsWalkable = false;
            }
        }
        
        // Notify pathfinding that the grid has changed
        if (pathfindingManager != null)
        {
            pathfindingManager.RecalculatePath();
        }
    }
}