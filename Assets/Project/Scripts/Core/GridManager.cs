using System.Collections.Generic;
using UnityEngine;

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
    
    [Header("Optimization")]
    [Tooltip("Should cell visualizers be pooled?")]
    [SerializeField] private bool useCellPooling = true;
    
    [Tooltip("Maximum number of cells to instantiate when pooling")]
    [SerializeField] private int maxPoolSize = 100;

    // The grid data structure that tracks cell occupancy and properties
    private GridCell[,] grid;
    
    // Dictionary to track cell visualizers
    private Dictionary<Vector2Int, GameObject> cellVisualizers = new Dictionary<Vector2Int, GameObject>();
    
    // Object pool for cell visualizers
    private Queue<GameObject> cellVisualizerPool = new Queue<GameObject>();
    
    // Reference to PathfindingManager (to be implemented)
    private PathfindingManager pathfindingManager;
    
    // Currently highlighted cell
    private Vector2Int? highlightedCell = null;
    
    // Reference to current path visualized
    private List<Vector2Int> currentPath = new List<Vector2Int>();

    // Flag to show/hide the grid
    private bool isGridVisible = false;

    /// <summary>
    /// Information about a cell in the grid
    /// </summary>
    public class GridCell
    {
        public bool IsOccupied { get; set; }
        public bool IsWalkable { get; set; }
        public GameObject OccupyingObject { get; set; }

        public GridCell(bool isWalkable = true)
        {
            IsOccupied = false;
            IsWalkable = isWalkable;
            OccupyingObject = null;
        }
    }

    private void Awake()
    {
        // Initialize the grid
        InitializeGrid();
        
        // Try to find the pathfinding manager in the scene
        pathfindingManager = FindObjectOfType<PathfindingManager>();
    }

    private void Start()
    {
        // Create cell visualizers but don't show them initially
        Debug.Log($"Grid dimensions: {gridWidth}x{gridHeight}");
        CreateCellVisualizers();
        SetGridVisibility(false);
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
                grid[x, y] = new GridCell();
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

        // If using pooling, create a pool of reusable cell visualizers
        if (useCellPooling)
        {
            int poolSize = Mathf.Min(gridWidth * gridHeight, maxPoolSize);
            
            for (int i = 0; i < poolSize; i++)
            {
                GameObject cell = Instantiate(cellPrefab, transform);
                cell.SetActive(false);
                cellVisualizerPool.Enqueue(cell);
            }
            
            Debug.Log($"Created cell visualizer pool with {poolSize} objects");
        }
        else
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
                        renderer.material = defaultCellMaterial;
                    }
                    
                    cell.name = $"Cell_{x}_{y}";
                    cellVisualizers.Add(new Vector2Int(x, y), cell);
                }
            }
        }
    }

    /// <summary>
    /// Shows or hides the visualized grid
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
                    visualizer.SetActive(false);
                    cellVisualizerPool.Enqueue(visualizer);
                }
                cellVisualizers.Clear();
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

    /// <summary>
    /// Converts a grid position to a world position
    /// </summary>
    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(
            gridPosition.x * cellSize + cellSize / 2f, 
            0.05f, // Slight elevation to avoid z-fighting
            gridPosition.y * cellSize + cellSize / 2f
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
                renderer.material = isValid ? validPlacementMaterial : invalidPlacementMaterial;
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
                    renderer.material = defaultCellMaterial;
                }
            }
            else if (useCellPooling)
            {
                // If using pooling and grid not visible, return to pool
                cellVisualizer.SetActive(false);
                cellVisualizerPool.Enqueue(cellVisualizer);
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
                        renderer.material = pathMaterial;
                    }
                    
                    // Ensure the cell is visible
                    cellVisualizer.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// Gets or creates a cell visualizer for the specified grid position
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
            
            // Otherwise, get one from the pool
            if (cellVisualizerPool.Count > 0)
            {
                GameObject cellVisualizer = cellVisualizerPool.Dequeue();
                
                // Position the visualizer
                Vector3 worldPosition = GetWorldPosition(gridPosition);
                cellVisualizer.transform.position = worldPosition;
                
                // Ensure proper scale
                cellVisualizer.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
                
                // Reset material
                Renderer renderer = cellVisualizer.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = defaultCellMaterial;
                }
                
                cellVisualizer.name = $"Cell_{gridPosition.x}_{gridPosition.y}";
                cellVisualizers.Add(gridPosition, cellVisualizer);
                
                return cellVisualizer;
            }
            
            // If the pool is empty, create a new visualizer (should be rare)
            Vector3 newPosition = GetWorldPosition(gridPosition);
            GameObject newCell = Instantiate(cellPrefab, newPosition, Quaternion.identity, transform);
            newCell.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
            newCell.name = $"Cell_{gridPosition.x}_{gridPosition.y}";
            cellVisualizers.Add(gridPosition, newCell);
            
            return newCell;
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
            return null;
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