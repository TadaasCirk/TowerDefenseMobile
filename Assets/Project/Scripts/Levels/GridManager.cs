using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 15;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Color validPlacementColor = Color.green;
    [SerializeField] private Color invalidPlacementColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugVisuals = true;
    
    private GridCell[,] grid;
    private GameObject[,] debugVisuals;
    
    private void Awake()
    {
        InitializeGrid();
    }
    
    private void InitializeGrid()
    {
        grid = new GridCell[width, height];
        
        // Create the grid data structure
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = new Vector3(x * cellSize, 0, y * cellSize);
                grid[x, y] = new GridCell(worldPos, new Vector2Int(x, y), true);
            }
        }
        
        if (showDebugVisuals)
            CreateDebugVisuals();
    }
    
    private void CreateDebugVisuals()
    {
        debugVisuals = new GameObject[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cellVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cellVisual.transform.position = grid[x, y].WorldPosition + Vector3.up * 0.01f;
                cellVisual.transform.rotation = Quaternion.Euler(90, 0, 0);
                cellVisual.transform.localScale = new Vector3(cellSize * 0.9f, cellSize * 0.9f, 1);
                cellVisual.transform.parent = transform;
                cellVisual.name = $"Cell_{x}_{y}";
                
                // Apply material or color
                Renderer renderer = cellVisual.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Unlit/Color"));
                renderer.material.color = defaultColor;
                renderer.material.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, 0.3f);
                
                debugVisuals[x, y] = cellVisual;
            }
        }
    }
    
    public GridCell GetCellFromWorldPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / cellSize);
        int y = Mathf.FloorToInt(worldPosition.z / cellSize);
        
        // Check if within bounds
        if (x >= 0 && x < width && y >= 0 && y < height)
            return grid[x, y];
            
        return null;
    }
    
    public bool CanPlaceTowerAt(Vector3 worldPosition)
    {
        GridCell cell = GetCellFromWorldPosition(worldPosition);
        return cell != null && cell.IsWalkable;
    }
    
    public void SetCellWalkable(int x, int y, bool isWalkable)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            grid[x, y].IsWalkable = isWalkable;
            
            if (showDebugVisuals && debugVisuals[x, y] != null)
            {
                debugVisuals[x, y].GetComponent<Renderer>().material.color = 
                    isWalkable ? defaultColor : invalidPlacementColor;
            }
        }
    }
}

[System.Serializable]
public class GridCell
{
    public Vector3 WorldPosition { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public bool IsWalkable { get; set; }
    
    public GridCell(Vector3 worldPos, Vector2Int gridPos, bool isWalkable)
    {
        WorldPosition = worldPos;
        GridPosition = gridPos;
        IsWalkable = isWalkable;
    }
}