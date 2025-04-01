using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Information about a cell in the grid using a struct for better memory performance
/// </summary>
public struct GridCell
{
    // Using bit flags for state to reduce memory usage
    private byte flags;
    public GameObject OccupyingObject { get; set; }

    // Bit flag constants
    private const byte OCCUPIED_FLAG = 1;
    private const byte WALKABLE_FLAG = 2;

    public GridCell(bool isWalkable = true)
    {
        flags = 0;
        if (isWalkable) flags |= WALKABLE_FLAG;
        OccupyingObject = null;
    }

    public bool IsOccupied
    {
        get => (flags & OCCUPIED_FLAG) != 0;
        set
        {
            if (value) flags |= OCCUPIED_FLAG;
            else flags &= unchecked((byte)~OCCUPIED_FLAG);
        }
    }

    public bool IsWalkable
    {
        get => (flags & WALKABLE_FLAG) != 0;
        set
        {
            if (value) flags |= WALKABLE_FLAG;
            else flags &= unchecked((byte)~WALKABLE_FLAG);
        }
    }
}

/// <summary>
/// Object pool for efficient reuse of grid cell visualizers
/// </summary>
public class GridObjectPool
{
    private Queue<GameObject> pool = new Queue<GameObject>();
    private GameObject prefab;
    private Transform parent;
    private int initialSize;
    private int maxSize;
    
    public GridObjectPool(GameObject prefab, Transform parent, int initialSize, int maxSize)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.initialSize = initialSize;
        this.maxSize = maxSize;
        Initialize();
    }
    
    private void Initialize()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateObject();
        }
    }
    
    private GameObject CreateObject()
    {
        GameObject obj = Object.Instantiate(prefab, parent);
        obj.SetActive(false);
        pool.Enqueue(obj);
        return obj;
    }
    
    public GameObject Get()
    {
        GameObject obj;
        
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else if (pool.Count + 1 <= maxSize)
        {
            obj = CreateObject();
            pool.Dequeue(); // Remove the object we just added
        }
        else
        {
            // If we've reached the max pool size, create a temporary object
            obj = Object.Instantiate(prefab, parent);
            Debug.LogWarning("Object pool capacity reached. Consider increasing the max size.");
        }
        
        obj.SetActive(true);
        return obj;
    }
    
    public void Return(GameObject obj)
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        
        // Don't add beyond max size
        if (pool.Count < maxSize)
        {
            pool.Enqueue(obj);
        }
        else
        {
            Object.Destroy(obj);
        }
    }
    
    public void Clear()
    {
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }
    }
    
    public int GetActiveCount()
    {
        return initialSize - pool.Count;
    }
}

/// <summary>
/// Spatial partitioning for efficient cell lookup
/// </summary>
public class GridSpatialPartitioning
{
    private int gridWidth;
    private int gridHeight;
    private int numPartitionsX;
    private int numPartitionsY;
    private const int PARTITION_SIZE = 4; // Each partition covers 4x4 grid cells
    
    private List<Vector2Int>[,] partitions;
    
    public GridSpatialPartitioning(int gridWidth, int gridHeight)
    {
        this.gridWidth = gridWidth;
        this.gridHeight = gridHeight;
        
        // Calculate number of partitions needed
        numPartitionsX = Mathf.CeilToInt((float)gridWidth / PARTITION_SIZE);
        numPartitionsY = Mathf.CeilToInt((float)gridHeight / PARTITION_SIZE);
        
        // Initialize partitions
        partitions = new List<Vector2Int>[numPartitionsX, numPartitionsY];
        
        for (int x = 0; x < numPartitionsX; x++)
        {
            for (int y = 0; y < numPartitionsY; y++)
            {
                partitions[x, y] = new List<Vector2Int>();
            }
        }
        
        // Populate partitions with all grid positions
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                AddToPartition(pos);
            }
        }
    }
    
    private void AddToPartition(Vector2Int position)
    {
        int partitionX = position.x / PARTITION_SIZE;
        int partitionY = position.y / PARTITION_SIZE;
        
        if (partitionX >= 0 && partitionX < numPartitionsX && 
            partitionY >= 0 && partitionY < numPartitionsY)
        {
            partitions[partitionX, partitionY].Add(position);
        }
    }
    
    public List<Vector2Int> GetCellsInRadius(Vector2Int center, int radius)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        
        // Determine which partitions to check
        int minPartitionX = Mathf.Max(0, (center.x - radius) / PARTITION_SIZE);
        int maxPartitionX = Mathf.Min(numPartitionsX - 1, (center.x + radius) / PARTITION_SIZE);
        int minPartitionY = Mathf.Max(0, (center.y - radius) / PARTITION_SIZE);
        int maxPartitionY = Mathf.Min(numPartitionsY - 1, (center.y + radius) / PARTITION_SIZE);
        
        // Check all cells in relevant partitions
        for (int px = minPartitionX; px <= maxPartitionX; px++)
        {
            for (int py = minPartitionY; py <= maxPartitionY; py++)
            {
                foreach (Vector2Int pos in partitions[px, py])
                {
                    int distSq = (pos.x - center.x) * (pos.x - center.x) + 
                                 (pos.y - center.y) * (pos.y - center.y);
                    
                    if (distSq <= radius * radius)
                    {
                        result.Add(pos);
                    }
                }
            }
        }
        
        return result;
    }
    
    public List<Vector2Int> GetCellsInView(Camera camera, float cellSize)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        
        if (camera == null) return result;
        
        // Get camera frustum
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        
        // Check all partitions against frustum
        for (int px = 0; px < numPartitionsX; px++)
        {
            for (int py = 0; py < numPartitionsY; py++)
            {
                // Create bounds for this partition
                Bounds partitionBounds = new Bounds(
                    new Vector3((px * PARTITION_SIZE + PARTITION_SIZE/2) * cellSize, 0, 
                               (py * PARTITION_SIZE + PARTITION_SIZE/2) * cellSize),
                    new Vector3(PARTITION_SIZE * cellSize, 0.1f, PARTITION_SIZE * cellSize));
                
                if (GeometryUtility.TestPlanesAABB(planes, partitionBounds))
                {
                    // If partition is visible, add all its cells
                    result.AddRange(partitions[px, py]);
                }
            }
        }
        
        return result;
    }
    
    // Find cells visible from a specific position (like for tower placement radius)
    public List<Vector2Int> GetCellsVisibleFrom(Vector2Int position, int maxDistance)
    {
        // This is essentially a radius check with a distance limit
        return GetCellsInRadius(position, maxDistance);
    }
    
    // For finding cells near a specific point quickly (optimizes tower targeting)
    public List<Vector2Int> GetCellsNear(Vector3 worldPosition, float cellSize, int radius)
    {
        // Convert world position to grid position
        int gridX = Mathf.FloorToInt(worldPosition.x / cellSize);
        int gridY = Mathf.FloorToInt(worldPosition.z / cellSize);
        
        return GetCellsInRadius(new Vector2Int(gridX, gridY), radius);
    }
    
    // Get all cells in a specific partition
    public List<Vector2Int> GetCellsInPartition(int partitionX, int partitionY)
    {
        if (partitionX < 0 || partitionX >= numPartitionsX || 
            partitionY < 0 || partitionY >= numPartitionsY)
        {
            return new List<Vector2Int>();
        }
        
        return new List<Vector2Int>(partitions[partitionX, partitionY]);
    }
    
    // Get the partition coordinates for a grid position
    public Vector2Int GetPartitionForCell(Vector2Int gridPosition)
    {
        return new Vector2Int(
            gridPosition.x / PARTITION_SIZE,
            gridPosition.y / PARTITION_SIZE
        );
    }
}