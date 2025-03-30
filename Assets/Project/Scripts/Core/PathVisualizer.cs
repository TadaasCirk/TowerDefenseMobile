using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] private PathfindingManager pathfindingManager;
    [SerializeField] private float lineHeight = 0.2f; // Height above ground
    [SerializeField] private float lineWidth = 0.3f;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = Color.cyan;
    
    private LineRenderer lineRenderer;
    
    private void Awake()
    {
        // Add LineRenderer if it doesn't exist
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // Configure LineRenderer
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.useWorldSpace = true;
    }
    
    private void Start()
    {
        // Find PathfindingManager if not assigned
        if (pathfindingManager == null)
        {
            pathfindingManager = FindObjectOfType<PathfindingManager>();
        }
        
        // Subscribe to path update events or request initial path
        UpdatePathVisualization();
    }
    
    public void UpdatePathVisualization()
    {
        if (pathfindingManager == null || lineRenderer == null)
            return;
            
        // Get current path from PathfindingManager
        List<Vector2Int> path = pathfindingManager.GetCurrentPath();
        
        if (path == null || path.Count == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }
        
        // Set up LineRenderer points
        lineRenderer.positionCount = path.Count;
        
        // Position each point along the path
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 worldPos = GetWorldPosFromGridPos(path[i]);
            worldPos.y = lineHeight; // Set consistent height
            lineRenderer.SetPosition(i, worldPos);
        }
    }
    
    private Vector3 GetWorldPosFromGridPos(Vector2Int gridPos)
    {
        // If using GridManager for this conversion
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            return gridManager.GetWorldPosition(gridPos);
        }
        
        // Simple fallback if no grid manager
        return new Vector3(gridPos.x, 0, gridPos.y);
    }
}