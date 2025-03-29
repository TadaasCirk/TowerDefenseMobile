using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// Ensures only one instance exists and provides global access.
    /// </summary>
    /// <typeparam name="T">The type of the singleton class</typeparam>
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        /// <summary>
        /// The static reference to the singleton instance
        /// </summary>
        private static T _instance;

        /// <summary>
        /// Flag to track whether the application is quitting
        /// This prevents singleton creation during application shutdown
        /// </summary>
        private static bool _isQuitting = false;

        /// <summary>
        /// Global access point to the singleton instance
        /// </summary>
        public static T Instance
        {
            get
            {
                // Check if we're quitting, in which case we should return null
                if (_isQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T).Name} already destroyed on application quit. Returning null.");
                    return null;
                }

                // If we don't have an instance, try to find one in the scene
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();

                    // If no instance exists in the scene, create a new GameObject with the component
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                        _instance = singletonObject.AddComponent<T>();
                        
                        Debug.Log($"[Singleton] Created new instance of {typeof(T).Name}");
                    }
                    else
                    {
                        Debug.Log($"[Singleton] Using existing instance of {typeof(T).Name}");
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Check if instance already exists when the object wakes up
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                // This is the first instance - make it the singleton
                _instance = (T)this;
                
                // If this object is a child, we need to make it a root object for DontDestroyOnLoad to work
                if (transform.parent != null)
                {
                    Debug.Log($"[Singleton] Detaching {typeof(T).Name} from parent for DontDestroyOnLoad");
                    transform.SetParent(null, true);
                }
                
                // Keep the singleton object alive across scene changes
                DontDestroyOnLoad(gameObject);
                
                // Call the initialization method
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                // Another instance already exists - destroy this one
                Debug.LogWarning($"[Singleton] Another instance of {typeof(T).Name} already exists! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Virtual method called when the singleton instance is created
        /// Override this in derived classes for initialization instead of Awake
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// Set the quitting flag when the application is quitting
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }
        
        /// <summary>
        /// Helper method to manually destroy the singleton instance.
        /// Useful for testing or scene reloading scenarios.
        /// </summary>
        public static void DestroySingleton()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}