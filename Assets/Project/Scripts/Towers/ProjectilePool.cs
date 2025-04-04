using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// Object pool for projectiles to avoid runtime instantiation
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        [System.Serializable]
        public class ProjectileType
        {
            public string id;
            public GameObject prefab;
            public int initialPoolSize = 20;
        }
        
        [Tooltip("Projectile types to pre-instantiate")]
        [SerializeField] private List<ProjectileType> projectileTypes = new List<ProjectileType>();
        
        [Tooltip("Should the pool expand if needed?")]
        [SerializeField] private bool canExpand = true;
        
        // Dictionary of projectile pools by ID
        private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        
        // Dictionary to track active projectiles
        private Dictionary<string, HashSet<GameObject>> activeProjectiles = new Dictionary<string, HashSet<GameObject>>();
        
        // Track pool sizes for stats/debugging
        private Dictionary<string, int> poolSizes = new Dictionary<string, int>();
        
        // Singleton instance
        private static ProjectilePool _instance;
        public static ProjectilePool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ProjectilePool>();
                    
                    if (_instance == null)
                    {
                        GameObject poolObj = new GameObject("ProjectilePool");
                        _instance = poolObj.AddComponent<ProjectilePool>();
                    }
                }
                
                return _instance;
            }
        }
        
        private void Awake()
        {
            // Ensure singleton behavior
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            if (persistBetweenScenes)
                DontDestroyOnLoad(gameObject);
                
            // Initialize pools
            InitializePools();
        }
        
        [Tooltip("Should the pool persist between scenes?")]
        [SerializeField] private bool persistBetweenScenes = true;
        
        /// <summary>
        /// Pre-instantiate all projectile types
        /// </summary>
        private void InitializePools()
        {
            foreach (var type in projectileTypes)
            {
                if (type.prefab == null)
                {
                    Debug.LogError($"ProjectilePool: Prefab is null for type {type.id}");
                    continue;
                }
                
                // Create pool for this projectile type
                Queue<GameObject> pool = new Queue<GameObject>();
                HashSet<GameObject> active = new HashSet<GameObject>();
                
                pools[type.id] = pool;
                activeProjectiles[type.id] = active;
                poolSizes[type.id] = type.initialPoolSize;
                
                // Pre-instantiate projectiles
                for (int i = 0; i < type.initialPoolSize; i++)
                {
                    GameObject obj = CreateProjectile(type.prefab, type.id);
                    pool.Enqueue(obj);
                }
                
                Debug.Log($"ProjectilePool: Pool initialized for {type.id} with {type.initialPoolSize} projectiles");
            }
        }
        
        /// <summary>
        /// Create a new projectile instance and set up
        /// </summary>
        private GameObject CreateProjectile(GameObject prefab, string poolId)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            obj.name = $"{prefab.name}_pooled";
            
            // Add a component to handle returning to pool
            ProjectilePoolItem poolItem = obj.AddComponent<ProjectilePoolItem>();
            poolItem.Initialize(poolId);
            
            return obj;
        }
        
        /// <summary>
        /// Get a projectile from the pool
        /// </summary>
        public GameObject GetProjectile(string projectileId)
        {
            // Check if pool exists for this projectile type
            if (!pools.TryGetValue(projectileId, out Queue<GameObject> pool))
            {
                Debug.LogError($"ProjectilePool: No pool found for projectile type {projectileId}");
                return null;
            }
            
            GameObject projectile;
            
            // If pool is empty, either expand or reuse the oldest active one
            if (pool.Count == 0)
            {
                if (canExpand)
                {
                    // Find the corresponding projectile type
                    ProjectileType type = projectileTypes.Find(t => t.id == projectileId);
                    
                    if (type != null && type.prefab != null)
                    {
                        // Create a new projectile
                        projectile = CreateProjectile(type.prefab, projectileId);
                        poolSizes[projectileId]++;
                        
                        Debug.Log($"ProjectilePool: Expanded pool for {projectileId}, new size: {poolSizes[projectileId]}");
                    }
                    else
                    {
                        Debug.LogError($"ProjectilePool: Could not expand pool for {projectileId}, projectile type not found");
                        return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"ProjectilePool: Pool for {projectileId} is empty and cannot expand");
                    return null;
                }
            }
            else
            {
                // Get projectile from pool
                projectile = pool.Dequeue();
            }
            
            // Add to active set
            activeProjectiles[projectileId].Add(projectile);
            
            // Activate projectile
            projectile.SetActive(true);
            
            return projectile;
        }
        
        /// <summary>
        /// Return a projectile to the pool
        /// </summary>
        public void ReturnProjectile(GameObject projectile, string poolId)
        {
            if (projectile == null)
                return;
                
            // Remove from active set
            if (activeProjectiles.TryGetValue(poolId, out HashSet<GameObject> active))
            {
                active.Remove(projectile);
            }
            
            // Reset projectile state
            projectile.SetActive(false);
            projectile.transform.SetParent(transform);
            
            // Return to pool
            if (pools.TryGetValue(poolId, out Queue<GameObject> pool))
            {
                pool.Enqueue(projectile);
            }
            else
            {
                Debug.LogWarning($"ProjectilePool: Attempted to return projectile to non-existent pool {poolId}");
                Destroy(projectile);
            }
        }
        
        /// <summary>
        /// Get the number of active projectiles of a specific type
        /// </summary>
        public int GetActiveCount(string projectileId)
        {
            if (activeProjectiles.TryGetValue(projectileId, out HashSet<GameObject> active))
            {
                return active.Count;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Get the total pool size for a specific projectile type
        /// </summary>
        public int GetPoolSize(string projectileId)
        {
            if (poolSizes.TryGetValue(projectileId, out int size))
            {
                return size;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Clean up and clear all pools
        /// </summary>
        public void ClearPools()
        {
            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Dequeue();
                    Destroy(obj);
                }
            }
            
            foreach (var active in activeProjectiles.Values)
            {
                foreach (GameObject obj in active)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                
                active.Clear();
            }
            
            pools.Clear();
            activeProjectiles.Clear();
            poolSizes.Clear();
        }
        
        private void OnDestroy()
        {
            ClearPools();
            _instance = null;
        }
    }
    
    /// <summary>
    /// Helper component attached to pooled projectiles to handle returning to pool
    /// </summary>
    public class ProjectilePoolItem : MonoBehaviour
    {
        public string PoolId { get; private set; }
        
        /// <summary>
        /// Initialize with the pool ID this projectile belongs to
        /// </summary>
        public void Initialize(string poolId)
        {
            PoolId = poolId;
        }
        
        /// <summary>
        /// Return to pool when disabled
        /// </summary>
        private void OnDisable()
        {
            if (ProjectilePool.Instance != null)
            {
                ProjectilePool.Instance.ReturnProjectile(gameObject, PoolId);
            }
        }
    }
}