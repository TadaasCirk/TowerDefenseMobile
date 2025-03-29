using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Core
{
    /// <summary>
    /// Core application manager that persists throughout the game
    /// Acts as a service locator and manages application lifecycle
    /// </summary>
    public class AppManager : SingletonBehaviour<AppManager>
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _showDebugInfo = true;
        
        [Header("Application Settings")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _multiTouchEnabled = true;
        
        [Header("Service Locator")]
        [SerializeField] private bool _enableServiceLocatorLogging = false;
        
        // Services collection
        private bool _servicesInitialized = false;
        
        /// <summary>
        /// Initialization method called when the singleton instance is created
        /// Called by SingletonBehaviour<T> during Awake
        /// </summary>
        protected override void OnSingletonAwake()
        {
            // Configure application settings
            ApplyApplicationSettings();
            
            // Configure service locator
            ServiceLocator.SetLogging(_enableServiceLocatorLogging);
            
            // Register as a service first (already done by SingletonBehaviour, but being explicit)
            ServiceLocator.Register<AppManager>(this, true);
            
            // Initialize all required services
            InitializeServices();
        }
        
        /// <summary>
        /// Initialize all core services needed by the application
        /// </summary>
        private void InitializeServices()
        {
            if (_servicesInitialized)
                return;
            
            // Log service initialization
            Debug.Log("AppManager: Initializing core services...");
            
            // Note: Services will register themselves with the ServiceLocator
            // This is just a convenient place to log/track service initialization
            
            _servicesInitialized = true;
        }
        
        private void ApplyApplicationSettings()
        {
            // Set target frame rate
            Application.targetFrameRate = _targetFrameRate;
            
            // Configure multi-touch
            Input.multiTouchEnabled = _multiTouchEnabled;
            
            // Prevent screen dimming
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            
            // Log application configuration
            Debug.Log($"Application configured: " +
                      $"Target FPS: {_targetFrameRate}, " +
                      $"Multi-touch: {_multiTouchEnabled}");
        }
        
        /// <summary>
        /// Load a scene by name with optional loading screen
        /// </summary>
        /// <param name="sceneName">Scene to load</param>
        /// <param name="useLoadingScreen">Whether to show loading screen</param>
        public void LoadScene(string sceneName, bool useLoadingScreen = false)
        {
            if (useLoadingScreen)
            {
                // TODO: Implement loading screen logic
                Debug.Log($"Loading scene with loading screen: {sceneName}");
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.Log($"Loading scene directly: {sceneName}");
                SceneManager.LoadScene(sceneName);
            }
        }
        
        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Quitting application");
            Application.Quit();
            
            // Also stop play mode in the editor
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
        
        /// <summary>
        /// OnDestroy is called when the behavior is destroyed
        /// </summary>
        protected override void OnDestroy()
        {
            // Clean up any resources
            
            // Call base implementation to unregister from ServiceLocator
            base.OnDestroy();
        }
        
        /// <summary>
        /// OnApplicationQuit is called when the application quits
        /// </summary>
        protected override void OnApplicationQuit()
        {
            // Perform any application shutdown logic
            Debug.Log("AppManager: Application shutting down...");
            
            // Clear all services when the application quits
            ServiceLocator.Clear();
            
            // Call base implementation
            base.OnApplicationQuit();
        }
    }
}