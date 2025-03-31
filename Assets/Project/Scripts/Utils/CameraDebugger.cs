using UnityEngine;

public class CameraDebugger : MonoBehaviour
{
    void Start()
    {
        // List all cameras in the scene
        Camera[] allCameras = Camera.allCameras;
        Debug.Log($"Found {allCameras.Length} cameras in the scene:");
        
        foreach (Camera cam in allCameras)
        {
            Debug.Log($"Camera: {cam.name}, Orthographic: {cam.orthographic}, " +
                      $"Enabled: {cam.enabled}, GameObject Active: {cam.gameObject.activeInHierarchy}");
        }
        
        // Force main camera to perspective mode
        Camera main = Camera.main;
        if (main != null)
        {
            main.orthographic = false;
            Debug.Log($"Set main camera ({main.name}) to Perspective mode");
        }
    }
}