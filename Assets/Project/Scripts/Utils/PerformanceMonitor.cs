using UnityEngine;
using TMPro;

namespace TowerDefense.Utils
{
    /// <summary>
    /// Simple performance monitor for debugging and optimization
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _showInBuild = false;
        [SerializeField] private float _updateInterval = 0.5f;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI _statsText;
        
        private float _accum = 0;
        private int _frames = 0;
        private float _timeLeft;
        private float _fps;
        private float _avgFrameTime;
        
        private void Start()
        {
            // Only show in editor or if specifically enabled for builds
            if (!Application.isEditor && !_showInBuild)
            {
                gameObject.SetActive(false);
                return;
            }
            
            _timeLeft = _updateInterval;
            
            // Ensure we have a text component
            if (_statsText == null)
            {
                Debug.LogWarning("PerformanceMonitor: No TextMeshProUGUI assigned");
                enabled = false;
            }
        }
        
        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            _accum += Time.timeScale / Time.deltaTime;
            _frames++;
            
            // Update stats at specified interval
            if (_timeLeft <= 0.0)
            {
                _fps = _accum / _frames;
                _avgFrameTime = 1000.0f / _fps;
                
                string stats = $"FPS: {_fps:F1}\n" +
                               $"Frame Time: {_avgFrameTime:F1}ms\n" +
                               $"Memory: {(System.GC.GetTotalMemory(false) / 1048576f):F1} MB";
                
                _statsText.text = stats;
                
                _timeLeft = _updateInterval;
                _accum = 0;
                _frames = 0;
            }
        }
    }
}