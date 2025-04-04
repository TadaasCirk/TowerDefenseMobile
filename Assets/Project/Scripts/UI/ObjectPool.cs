using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Generic object pool for reusing GameObjects
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [System.Serializable]
        public class PoolItem
        {
            public string id;
            public GameObject prefab;
            public int initialCount;
            public Transform container;
        }
        
        [Tooltip("Items to pre-pool")]
        [SerializeField] private List<PoolItem> poolItems = new List<PoolItem>();
        
        // Dictionary of pools
        private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        
        // Singleton instance
        private static ObjectPool _instance;
        
        public static ObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ObjectPool>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("ObjectPool");
                        _instance = obj.AddComponent<ObjectPool>();
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
            DontDestroyOnLoad(gameObject);
            
            // Initialize pools
            InitializePools();
        }
        
        /// <summary>
        /// Pre-instantiate objects in pools
        /// </summary>
        private void InitializePools()
        {
            foreach (var item in poolItems)
            {
                if (item.prefab == null)
                {
                    Debug.LogWarning($"Object Pool: Prefab is null for pool item {item.id}");
                    continue;
                }
                
                // Create container if needed
                if (item.container == null)
                {
                    GameObject containerObj = new GameObject($"{item.id}_Container");
                    containerObj.transform.SetParent(transform);
                    item.container = containerObj.transform;
                }
                
                // Create queue
                Queue<GameObject> queue = new Queue<GameObject>();
                
                // Pre-instantiate objects
                for (int i = 0; i < item.initialCount; i++)
                {
                    GameObject obj = CreatePooledObject(item.prefab, item.container);
                    queue.Enqueue(obj);
                }
                
                pools[item.id] = queue;
                
                Debug.Log($"Object Pool: Created pool for {item.id} with {item.initialCount} objects");
            }
        }
        
        /// <summary>
        /// Create a single pooled object
        /// </summary>
        private GameObject CreatePooledObject(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            
            // Add return to pool component
            ReturnToPool returnToPool = obj.AddComponent<ReturnToPool>();
            
            return obj;
        }
        
        /// <summary>
        /// Get an object from the pool
        /// </summary>
        public GameObject Get(string id, Vector3 position, Quaternion rotation)
        {
            if (!pools.ContainsKey(id))
            {
                Debug.LogWarning($"Object Pool: No pool with id {id} exists");
                return null;
            }
            
            Queue<GameObject> pool = pools[id];
            
            // If pool is empty, expand it
            if (pool.Count == 0)
            {
                PoolItem item = poolItems.Find(i => i.id == id);
                if (item != null && item.prefab != null)
                {
                    GameObject newObj = CreatePooledObject(item.prefab, item.container);
                    pool.Enqueue(newObj);
                    
                    Debug.Log($"Object Pool: Expanded pool {id}");
                }
                else
                {
                    Debug.LogWarning($"Object Pool: Could not expand pool {id}");
                    return null;
                }
            }
            
            // Get object from pool
            GameObject obj = pool.Dequeue();
            
            // Set position and rotation
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            
            // Activate
            obj.SetActive(true);
            
            // Get return to pool component
            ReturnToPool returnToPool = obj.GetComponent<ReturnToPool>();
            if (returnToPool != null)
            {
                returnToPool.PoolId = id;
            }
            
            return obj;
        }
        
        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(GameObject obj, string id)
        {
            if (obj == null) return;
            
            // Disable the object
            obj.SetActive(false);
            
            // Add to pool
            if (pools.ContainsKey(id))
            {
                pools[id].Enqueue(obj);
            }
            else
            {
                Debug.LogWarning($"Object Pool: Attempted to return object to non-existent pool {id}");
                Destroy(obj);
            }
        }
        
        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Dequeue();
                    Destroy(obj);
                }
            }
            
            pools.Clear();
        }
    }
    
    /// <summary>
    /// Component to handle returning objects to the pool
    /// </summary>
    public class ReturnToPool : MonoBehaviour
    {
        public string PoolId { get; set; }
        
        public float AutoReturnTime { get; set; } = -1;
        
        private void OnEnable()
        {
            // If auto return time is set, start timer
            if (AutoReturnTime > 0)
            {
                StartCoroutine(AutoReturnCoroutine());
            }
        }
        
        private System.Collections.IEnumerator AutoReturnCoroutine()
        {
            yield return new WaitForSeconds(AutoReturnTime);
            ReturnToPoolNow();
        }
        
        public void ReturnToPoolNow()
        {
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Return(gameObject, PoolId);
            }
        }
    }
}