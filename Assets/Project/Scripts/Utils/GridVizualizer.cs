using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    [Header("Grid Visualization")]
    [SerializeField] private bool showGridInGame = true;
    [SerializeField] private Color gridLineColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    [SerializeField] private float lineWidth = 0.025f;
    [SerializeField] private float heightOffset = 0.01f; // Slightly above ground

    // References
    private GridManager gridManager;
    private GameObject gridLineContainer;

    void Start()
    {
        // Find GridManager
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GridVisualizer: Could not find GridManager!");
            return;
        }

        // Create grid lines
        if (showGridInGame)
        {
            CreateGridLines();
        }
    }

    void CreateGridLines()
    {
        // Get grid dimensions from GridManager
        Vector2Int dimensions = gridManager.GetGridDimensions();
        float cellSize = gridManager.GetCellSize();

        // Create a container for all grid lines
        gridLineContainer = new GameObject("GridLines");
        gridLineContainer.transform.position = Vector3.zero;

        // Calculate grid bounds
        float width = dimensions.x * cellSize;
        float length = dimensions.y * cellSize;
        
        // Get grid origin from GridManager's position
        Vector3 gridOrigin = transform.position;
        
        // Create horizontal lines (along X axis)
        for (int z = 0; z <= dimensions.y; z++)
        {
            float posZ = gridOrigin.z + z * cellSize;
            CreateLine(
                new Vector3(gridOrigin.x, heightOffset, posZ),
                new Vector3(gridOrigin.x + width, heightOffset, posZ)
            );
        }

        // Create vertical lines (along Z axis)
        for (int x = 0; x <= dimensions.x; x++)
        {
            float posX = gridOrigin.x + x * cellSize;
            CreateLine(
                new Vector3(posX, heightOffset, gridOrigin.z),
                new Vector3(posX, heightOffset, gridOrigin.z + length)
            );
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(gridLineContainer.transform);

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPositions(new Vector3[] { start, end });
        
        // Configure line appearance
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        line.material.color = gridLineColor;
    }
}