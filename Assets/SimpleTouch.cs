using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleTouch : MonoBehaviour
{
    void Update()
    {
        // Check for mouse click/touch using the new Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Get mouse position
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10));
            
            Debug.Log($"Click detected at world position: {worldPos}");
            
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = worldPos;
            sphere.transform.localScale = Vector3.one * 0.3f;
            sphere.GetComponent<Renderer>().material.color = Color.red;
            
            Destroy(sphere, 1f);
        }
        
        // Also check for touches (for mobile)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, 10));
            
            Debug.Log($"Touch detected at world position: {worldPos}");
            
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = worldPos;
            sphere.transform.localScale = Vector3.one * 0.3f;
            sphere.GetComponent<Renderer>().material.color = Color.blue;
            
            Destroy(sphere, 1f);
        }
    }
}