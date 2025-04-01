using UnityEngine;

public class CameraInitializer : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private bool forcePerspective = true;
    [SerializeField] private float fieldOfView = 60f;
    [SerializeField] private float nearClipPlane = 0.3f;
    [SerializeField] private float farClipPlane = 1000f;
    
    // Reference to camera component
    private Camera cameraComponent;
    
    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
        {
            Debug.LogError("No Camera component found on this GameObject");
            return;
        }
        
        // Apply initial settings
        ApplyCameraSettings();
    }
    
    void Start()
    {
        // Apply again in Start to override any settings that might be applied in Awake of other scripts
        ApplyCameraSettings();
        
        // Schedule one more application after a short delay to ensure it sticks
        Invoke("ApplyCameraSettings", 0.1f);
    }
    
    void ApplyCameraSettings()
    {
        if (cameraComponent == null) return;
        
        // Force perspective mode if required
        if (forcePerspective)
        {
            cameraComponent.orthographic = false;
        }
        
        // Apply other settings
        cameraComponent.fieldOfView = fieldOfView;
        cameraComponent.nearClipPlane = nearClipPlane;
        cameraComponent.farClipPlane = farClipPlane;
        
        // Force a refresh of the camera's projection matrix
        cameraComponent.ResetProjectionMatrix();
        
        Debug.Log($"Camera settings applied: Projection = {(cameraComponent.orthographic ? "Orthographic" : "Perspective")}, " +
                 $"FOV = {cameraComponent.fieldOfView}, Near = {cameraComponent.nearClipPlane}, Far = {cameraComponent.farClipPlane}");
    }
    
    // Add this method to test toggling settings at runtime
    public void RefreshCameraSettings()
    {
        ApplyCameraSettings();
    }
}