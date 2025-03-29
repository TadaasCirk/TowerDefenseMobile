using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Core
{
    /// <summary>
    /// Core application manager that persists throughout the game
    /// Acts as a service locator and manages application lifecycle
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        // Singleton instance
        public static AppManager Instance { get; private set; }
        
        [Header("Debug Settings")]
        [SerializeField] private bool _showDebugInfo = true;
        
        [Header("Application Settings")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _multiTouchEnabled = true;
        
        // Service references can be added here as needed
        
        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Configure application settings
            ApplyApplicationSettings();
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
    }
}