using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TowerDefense.Core;

/// <summary>
/// Manages pathfinding for the tower defense game using A* algorithm.
/// Calculates paths for enemies and validates tower placement.
/// </summary>
public class PathfindingManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the grid manager")]
    [SerializeField] private GridManager gridManager;

    [Tooltip("The entry point for enemies")]
    [SerializeField] private Transform entryPoint;

    [Tooltip("The exit point for enemies")]
    [SerializeField] private Transform exitPoint;

    [Header("Pathfinding Settings")]
    [Tooltip("Should diagonal movement be allowed?")]
    [SerializeField] private bool allowDiagonals = false;

    [Tooltip("The penalty for diagonal movement (if allowed)")]
    [SerializeField] private float diagonalMovementPenalty = 1.4f;

    [Tooltip("Visualize the path in the scene?")]
    [SerializeField] private bool visualizePath = true;

    // The current calculated path
    private List<Vector2Int> currentPath = new List<Vector2Int>();

    // The start and end positions in grid coordinates
    private Vector2Int startGridPos;
    private Vector2Int endGridPos;

    // Direction vectors for pathfinding
    private Vector2Int[] directions;

    // Track initialization state
    private bool isInitialized = false;

    // A node in the pathfinding grid
    private class PathNode
    {
        public Vector2Int Position { get; set; }
        public bool IsWalkable { get; set; }
        public float GCost { get; set; } // Cost from start to this node
        public float HCost { get; set; } // Heuristic cost from this node to end
        public float FCost => GCost + HCost; // Total cost
        public PathNode Parent { get; set; } // For path reconstruction

        public PathNode(Vector2Int position, bool isWalkable)
        {
            Position = position;
            IsWalkable = isWalkable;
            GCost = float.MaxValue;
            HCost = 0;
            Parent = null;
        }
    }

    private void Awake()
    {
        // Initialize direction vectors for pathfinding
        if (allowDiagonals)
        {
            directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // Up
                new Vector2Int(1, 0),   // Right
                new Vector2Int(0, -1),  // Down
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(1, 1),   // Up-Right
                new Vector2Int(1, -1),  // Down-Right
                new Vector2Int(-1, -1), // Down-Left
                new Vector2Int(-1, 1)   // Up-Left
            };
        }
        else
        {
            directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // Up
                new Vector2Int(1, 0),   // Right
                new Vector2Int(0, -1),  // Down
                new Vector2Int(-1, 0)   // Left
            };
        }
        
        // Register with ServiceLocator
        ServiceLocator.Register<PathfindingManager>(this);
    }

    private void Start()
    {
        // Find dependencies if not assigned in Inspector
        ResolveDependencies();

        // Calculate initial grid positions for entry and exit
        if (entryPoint != null && exitPoint != null && gridManager != null)
        {
            startGridPos = gridManager.GetGridPosition(entryPoint.position);
            endGridPos = gridManager.GetGridPosition(exitPoint.position);
            
            Debug.Log($"PathfindingManager: Entry point at grid position {startGridPos}, exit at {endGridPos}");
        }
        else
        {
            Debug.LogError("PathfindingManager: Missing required references for initialization!");
            return;
        }

        // Initial path calculation
        RecalculatePath();
        
        // Mark as initialized after initial calculation
        isInitialized = true;
        Debug.Log("PathfindingManager: Initialization complete");
    }
    
    private void OnDestroy()
    {
        // Unregister from ServiceLocator
        ServiceLocator.Unregister<PathfindingManager>();
    }
    
    /// <summary>
    /// Find any required dependencies not assigned in Inspector
    /// </summary>
    private void ResolveDependencies()
    {
        if (gridManager == null)
        {
            // Try to get from ServiceLocator first
            gridManager = ServiceLocator.Get<GridManager>(true);
            
            // Fallback to FindObjectOfType only if necessary
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
                
                if (gridManager == null)
                {
                    Debug.LogError("PathfindingManager: Could not find GridManager! Pathfinding will not function.");
                }
            }
        }
    }
    
    /// <summary>
    /// Checks if the pathfinding system is initialized and has a valid path
    /// </summary>
    public bool IsPathCalculated()
    {
        return isInitialized && currentPath != null && currentPath.Count > 0;
    }

    /// <summary>
    /// Recalculates the path from entry to exit point
    /// </summary>
    public void RecalculatePath()
    {
        if (gridManager == null || entryPoint == null || exitPoint == null)
        {
            Debug.LogWarning("PathfindingManager: Missing references for path calculation!");
            return;
        }

        // Calculate grid positions for entry and exit (in case they moved)
        startGridPos = gridManager.GetGridPosition(entryPoint.position);
        endGridPos = gridManager.GetGridPosition(exitPoint.position);

        // Find the path using A*
        List<Vector2Int> newPath = FindPath(startGridPos, endGridPos);
        
        if (newPath != null && newPath.Count > 0)
        {
            currentPath = newPath;
            Debug.Log($"PathfindingManager: Path recalculated with {currentPath.Count} nodes from {startGridPos} to {endGridPos}");
        }
        else
        {
            Debug.LogWarning("PathfindingManager: Failed to calculate a valid path!");
        }

        // Visualize the path if requested
        if (visualizePath && currentPath != null && currentPath.Count > 0 && gridManager != null)
        {
            gridManager.VisualizePath(currentPath);
        }
    }

    /// <summary>
    /// Find a path using the A* algorithm
    /// </summary>
    private List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int endPos)
    {
        // Check if start and end positions are valid
        if (!IsPositionValid(startPos) || !IsPositionValid(endPos))
        {
            Debug.LogWarning($"PathfindingManager: Invalid start ({startPos}) or end ({endPos}) position!");
            return null;
        }

        // Check if end position is walkable
        if (!IsPositionWalkable(endPos))
        {
            Debug.LogWarning($"PathfindingManager: End position {endPos} is not walkable!");
            return null;
        }

        // Initialize the grid of nodes
        Dictionary<Vector2Int, PathNode> grid = InitializePathfindingGrid();

        // Initialize open and closed sets
        HashSet<PathNode> openSet = new HashSet<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        // Add start node to the open set
        PathNode startNode = grid[startPos];
        startNode.GCost = 0;
        startNode.HCost = CalculateHeuristicCost(startPos, endPos);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // Find the node with the lowest F cost in the open set
            PathNode currentNode = GetLowestFCostNode(openSet);

            // If we've reached the end node, reconstruct the path
            if (currentNode.Position == endPos)
            {
                return ReconstructPath(currentNode);
            }

            // Move current node from open to closed set
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Check each neighbor of the current node
            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighborPos = currentNode.Position + direction;

                // Skip if outside grid or not walkable
                if (!IsPositionValid(neighborPos) || !IsPositionWalkable(neighborPos))
                {
                    continue;
                }

                // Skip if this neighbor is already in the closed set
                PathNode neighborNode = grid[neighborPos];
                if (closedSet.Contains(neighborNode))
                {
                    continue;
                }

                // Calculate the movement cost (diagonal movement costs more)
                float movementCost = 1f;
                if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) > 1)
                {
                    movementCost = diagonalMovementPenalty;
                }

                // Calculate the new G cost for this path to the neighbor
                float newGCost = currentNode.GCost + movementCost;

                // If this is a better path to the neighbor, update it
                if (newGCost < neighborNode.GCost)
                {
                    neighborNode.Parent = currentNode;
                    neighborNode.GCost = newGCost;
                    neighborNode.HCost = CalculateHeuristicCost(neighborPos, endPos);

                    // Add to open set if it's not already there
                    if (!openSet.Contains(neighborNode))
                    {
                        openSet.Add(neighborNode);
                    }
                }
            }
        }

        // If we get here, no path was found
        Debug.LogWarning("PathfindingManager: No path found!");
        return null;
    }

    /// <summary>
    /// Initialize the pathfinding grid based on the current state of the gameplay grid
    /// </summary>
    private Dictionary<Vector2Int, PathNode> InitializePathfindingGrid()
    {
        Dictionary<Vector2Int, PathNode> grid = new Dictionary<Vector2Int, PathNode>();
        
        if (gridManager != null)
        {
            Vector2Int dimensions = gridManager.GetGridDimensions();

            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    bool isWalkable = IsPositionWalkable(position);
                    grid[position] = new PathNode(position, isWalkable);
                }
            }
        }

        return grid;
    }

    /// <summary>
    /// Reconstruct the path by following parent nodes from end to start
    /// </summary>
    private List<Vector2Int> ReconstructPath(PathNode endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        PathNode currentNode = endNode;

        // Follow parent nodes back to the start
        while (currentNode != null)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }

        // Reverse the path to get start->end order
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Calculate the heuristic cost (Manhattan distance)
    /// </summary>
    private float CalculateHeuristicCost(Vector2Int from, Vector2Int to)
    {
        if (allowDiagonals)
        {
            // Chebyshev distance for when diagonals are allowed with the same cost
            return Mathf.Max(Mathf.Abs(from.x - to.x), Mathf.Abs(from.y - to.y));
        }
        else
        {
            // Manhattan distance for grid with only cardinal directions
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }
    }

    /// <summary>
    /// Find the node with the lowest F cost in a set
    /// </summary>
    private PathNode GetLowestFCostNode(HashSet<PathNode> nodeSet)
    {
        PathNode lowestNode = nodeSet.First();
        foreach (var node in nodeSet)
        {
            // Check for lower F cost, or equal F cost but lower H cost
            if (node.FCost < lowestNode.FCost || 
                (node.FCost == lowestNode.FCost && node.HCost < lowestNode.HCost))
            {
                lowestNode = node;
            }
        }
        return lowestNode;
    }

    /// <summary>
    /// Checks if the specified position is within the grid boundaries
    /// </summary>
    private bool IsPositionValid(Vector2Int position)
    {
        return gridManager != null && gridManager.IsWithinGrid(position);
    }

    /// <summary>
    /// Checks if the specified position is walkable
    /// </summary>
    private bool IsPositionWalkable(Vector2Int position)
    {
        if (gridManager == null)
            return false;
            
        GridManager.GridCell cell = gridManager.GetCell(position);
        return cell != null && cell.IsWalkable;
    }

    /// <summary>
    /// Checks if a path exists in the given grid (for tower placement validation)
    /// </summary>
    public bool CheckPathExists(GridManager.GridCell[,] simulatedGrid)
    {
        // Create a specialized path grid from the simulated grid
        Dictionary<Vector2Int, PathNode> pathGrid = new Dictionary<Vector2Int, PathNode>();
        
        // Get grid dimensions
        int width = simulatedGrid.GetLength(0);
        int height = simulatedGrid.GetLength(1);
        
        // Initialize path grid based on the simulated grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                bool isWalkable = simulatedGrid[x, y] != null && simulatedGrid[x, y].IsWalkable;
                pathGrid[position] = new PathNode(position, isWalkable);
            }
        }
        
        // Check if start and end positions are valid
        if (!IsPositionValid(startGridPos) || !IsPositionValid(endGridPos))
        {
            Debug.LogWarning("PathfindingManager: Start or end position is invalid for path check!");
            return false;
        }
        
        // Initialize open and closed sets
        HashSet<PathNode> openSet = new HashSet<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();
        
        // Add start node to the open set
        PathNode startNode = pathGrid[startGridPos];
        startNode.GCost = 0;
        startNode.HCost = CalculateHeuristicCost(startGridPos, endGridPos);
        openSet.Add(startNode);
        
        while (openSet.Count > 0)
        {
            // Find the node with the lowest F cost in the open set
            PathNode currentNode = GetLowestFCostNode(openSet);
            
            // If we've reached the end node, a path exists
            if (currentNode.Position == endGridPos)
            {
                return true;
            }
            
            // Move current node from open to closed set
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            // Check each neighbor of the current node
            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighborPos = currentNode.Position + direction;
                
                // Skip if outside grid
                if (!IsPositionValid(neighborPos))
                {
                    continue;
                }
                
                // Skip if this position is not walkable in the simulated grid
                PathNode neighborNode = pathGrid[neighborPos];
                if (!neighborNode.IsWalkable || closedSet.Contains(neighborNode))
                {
                    continue;
                }
                
                // Calculate the movement cost (diagonal movement costs more)
                float movementCost = 1f;
                if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) > 1)
                {
                    movementCost = diagonalMovementPenalty;
                }
                
                // Calculate the new G cost for this path to the neighbor
                float newGCost = currentNode.GCost + movementCost;
                
                // If this is a better path to the neighbor, update it
                if (newGCost < neighborNode.GCost)
                {
                    neighborNode.Parent = currentNode;
                    neighborNode.GCost = newGCost;
                    neighborNode.HCost = CalculateHeuristicCost(neighborPos, endGridPos);
                    
                    // Add to open set if it's not already there
                    if (!openSet.Contains(neighborNode))
                    {
                        openSet.Add(neighborNode);
                    }
                }
            }
        }
        
        // If we get here, no path was found
        return false;
    }

    /// <summary>
    /// Gets the current path
    /// </summary>
    public List<Vector2Int> GetCurrentPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning("PathfindingManager: Path requested but not yet calculated!");
            // Force path calculation if not done yet
            RecalculatePath();
        }
        
        // Return a defensive copy of the path to prevent external modification
        return new List<Vector2Int>(currentPath ?? new List<Vector2Int>());
    }

    /// <summary>
    /// Gets the entry position in grid coordinates
    /// </summary>
    public Vector2Int GetEntryGridPosition()
    {
        return startGridPos;
    }

    /// <summary>
    /// Gets the exit position in grid coordinates
    /// </summary>
    public Vector2Int GetExitGridPosition()
    {
        return endGridPos;
    }

    /// <summary>
    /// Sets the entry and exit points
    /// </summary>
    public void SetEntryExitPoints(Transform entry, Transform exit)
    {
        entryPoint = entry;
        exitPoint = exit;
        RecalculatePath();
    }

    /// <summary>
    /// Draw lines showing the path in the Scene view for debugging
    /// </summary>
    private void OnDrawGizmos()
    {
        // Only draw if we have a valid path and grid manager
        if (currentPath == null || currentPath.Count < 2 || gridManager == null)
            return;

        Gizmos.color = Color.yellow;

        // Draw lines between path points
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 startPos = gridManager.GetWorldPosition(currentPath[i]);
            Vector3 endPos = gridManager.GetWorldPosition(currentPath[i + 1]);
            
            // Raise the line slightly above the grid for visibility
            startPos.y += 0.1f;
            endPos.y += 0.1f;
            
            Gizmos.DrawLine(startPos, endPos);
        }

        // Draw entry and exit points
        if (entryPoint != null && exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(entryPoint.position, 0.3f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(exitPoint.position, 0.3f);
        }
    }
}