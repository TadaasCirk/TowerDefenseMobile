using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    public GameObject targetObject;
    
    void Update()
    {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane gridPlane = new Plane(Vector3.up, Vector3.zero);
        
        float distance;
        if (gridPlane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            
            // Move the target object
            if (targetObject != null)
            {
                targetObject.transform.position = new Vector3(hitPoint.x, 0, hitPoint.z);
            }
        }
    }
}