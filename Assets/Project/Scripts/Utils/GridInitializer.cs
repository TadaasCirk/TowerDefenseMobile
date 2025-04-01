using UnityEngine;

public class GridInitializer : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PathfindingManager pathfindingManager;
    
    [Header("Entry/Exit Points")]
    [SerializeField] private Vector2Int entryPoint = new Vector2Int(0, 16); // Left middle
    [SerializeField] private Vector2Int exitPoint = new Vector2Int(31, 16); // Right middle
    
    void Start()
    {
        if (gridManager == null)
            gridManager = GetComponent<GridManager>();
            
        if (pathfindingManager == null)
            pathfindingManager = FindObjectOfType<PathfindingManager>();
        
        if (gridManager == null || pathfindingManager == null)
        {
            Debug.LogError("Missing required components");
            return;
        }
        
        // Make sure entry and exit transforms are at correct positions
        SetupPathPoints();
        
        // Force height offset on grid for better visibility
        float heightOffset = 0.05f; // Just above ground
        gridManager.SetPathHeight(heightOffset);
    }
    
    private void SetupPathPoints()
    {
        // Convert grid positions to world positions
        Vector3 entryWorldPos = gridManager.GetWorldPosition(entryPoint);
        Vector3 exitWorldPos = gridManager.GetWorldPosition(exitPoint);
        
        // Ensure pathfinding entry/exit points are positioned correctly
        if (pathfindingManager.entryPoint != null)
            pathfindingManager.entryPoint.position = entryWorldPos;
            
        // Check if PathfindingManager has a setter method or public field for exit point
        // This depends on your implementation
        // pathfindingManager.exitPoint.position = exitWorldPos;
        
        // Alternative: use reflection or create a public setter method
    }
}