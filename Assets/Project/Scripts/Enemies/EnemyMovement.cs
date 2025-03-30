using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;

/// <summary>
/// Controls enemy movement along a path in the tower defense game.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base movement speed of the enemy")]
    [SerializeField] private float moveSpeed = 2.0f;
    
    [Tooltip("How close the enemy needs to be to a waypoint to move to the next one")]
    [SerializeField] private float waypointThreshold = 0.1f;
    
    [Tooltip("Should the enemy smoothly rotate to face movement direction?")]
    [SerializeField] private bool smoothRotation = true;
    
    [Tooltip("How quickly the enemy rotates to face movement direction")]
    [SerializeField] private float rotationSpeed = 10f;
    
    [Tooltip("Maximum path initialization attempts")]
    [SerializeField] private int maxInitAttempts = 3;
    
    [Tooltip("Delay between path initialization attempts")]
    [SerializeField] private float initRetryDelay = 0.2f;
    
    [Header("Effect Settings")]
    [Tooltip("Visual effect to play when the enemy reaches the end of the path")]
    [SerializeField] private GameObject reachEndEffectPrefab;
    
    // References
    private GridManager gridManager;
    private PathfindingManager pathfindingManager;
    
    // Path data
    private List<Vector2Int> pathWaypoints;
    private List<Vector3> worldWaypoints;
    private int currentWaypointIndex = 0;
    
    // Movement state
    private Vector3 currentMoveDirection;
    private bool pathComplete = false;
    private bool isMovementPaused = false;
    private bool isPathInitialized = false;
    private int initAttempts = 0;
    
    // Speed modifiers (for slowing effects from towers)
    private float speedModifier = 1.0f;
    private Dictionary<int, SpeedModifier> activeSpeedModifiers = new Dictionary<int, SpeedModifier>();
    
    // Class to track speed modifiers with IDs
    private class SpeedModifier
    {
        public int ID { get; private set; }
        public float Value { get; private set; }
        public float Duration { get; private set; }
        public float RemainingTime { get; set; }
        
        public SpeedModifier(int id, float value, float duration)
        {
            ID = id;
            Value = value;
            Duration = duration;
            RemainingTime = duration;
        }
    }
    
    private void Awake()
    {
        // We'll resolve dependencies in Start to ensure ServiceLocator is populated
    }
    
    private void Start()
    {
        Debug.Log($"Enemy {gameObject.name} initializing at position: {transform.position}");
        
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.parent = this.transform;
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = new Vector3(1, 1, 1);
        marker.GetComponent<Renderer>().material.color = Color.magenta;


        // Find required dependencies
        if (pathfindingManager == null)
            pathfindingManager = FindObjectOfType<PathfindingManager>();
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
            
        // Initialize path but don't override starting position
        StartCoroutine(InitializePathWithDelay());
    }
    
    /// <summary>
    /// Resolves dependencies using ServiceLocator with fallbacks
    /// </summary>
    private void ResolveDependencies()
    {
        // Try to get GridManager from ServiceLocator
        gridManager = ServiceLocator.Get<GridManager>(true);
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("EnemyMovement: Could not find GridManager!");
            }
        }
        
        // Try to get PathfindingManager from ServiceLocator
        pathfindingManager = ServiceLocator.Get<PathfindingManager>(true);
        if (pathfindingManager == null)
        {
            pathfindingManager = FindObjectOfType<PathfindingManager>();
            if (pathfindingManager == null)
            {
                Debug.LogWarning("EnemyMovement: Could not find PathfindingManager!");
            }
        }
    }
    
    /// <summary>
    /// Coroutine to try initializing the path with a delay, with multiple attempts if needed
    /// </summary>
    private IEnumerator InitializePathWithDelay()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure other systems are ready
        
        if (TryInitializePath())
        {
            isPathInitialized = true;
            Debug.Log($"Path initialized for {gameObject.name} with {worldWaypoints.Count} waypoints");
            
            // Important: Find the closest waypoint to start from rather than teleporting
            if (worldWaypoints != null && worldWaypoints.Count > 1)
            {
                // Find the closest waypoint to start from
                float closestDistance = float.MaxValue;
                int closestIndex = 0;
                
                for (int i = 0; i < worldWaypoints.Count; i++)
                {
                    float distance = Vector3.Distance(transform.position, worldWaypoints[i]);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIndex = i;
                    }
                }
                
                // If we're very close to the first waypoint, start from the next one
                if (closestIndex == 0 && closestDistance < 0.5f && worldWaypoints.Count > 1)
                {
                    currentWaypointIndex = 1;
                }
                else
                {
                    currentWaypointIndex = closestIndex;
                }
                
                Debug.Log($"Starting from waypoint index: {currentWaypointIndex}");
            }
        }
        else
        {
            Debug.LogError($"Failed to initialize path for {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Attempts to initialize the path from the pathfinding manager
    /// </summary>
    /// <returns>True if successful, false otherwise</returns>
    private bool TryInitializePath()
    {
        if (pathfindingManager == null || gridManager == null)
        {
            Debug.LogError("Missing required managers for path initialization!");
            return false;
        }
    
        // Get the path from the pathfinding manager - this line needs correcting
        List<Vector2Int> pathPoints = pathfindingManager.GetCurrentPath();
    
        if (pathPoints == null || pathPoints.Count < 2)
        {
            Debug.LogError($"Invalid path received for {gameObject.name}! Count: {pathPoints?.Count ?? 0}");
            return false;
        }
    
        // Convert grid positions to world positions with proper height
        worldWaypoints = new List<Vector3>();
        foreach (Vector2Int gridPos in pathPoints)
        {
            Vector3 worldPos = gridManager.GetWorldPosition(gridPos);
            // Use the same Y height as the enemy's initial position
            worldPos.y = transform.position.y;
            worldWaypoints.Add(worldPos);
        }
    
        // Don't teleport to first waypoint, just set the index to 0
        currentWaypointIndex = 0;
    
        return true;
    }

    // Add a fallback method to create a simple default path if pathfinding fails
    private void CreateDefaultPath()
    {
        // Create a simple straight-line path as fallback
        pathWaypoints = new List<Vector2Int>();
        worldWaypoints = new List<Vector3>();
    
        // Add current position as first waypoint
        Vector3 startPos = transform.position;
        worldWaypoints.Add(startPos);
    
        // Add a point 10 units forward as second waypoint
        Vector3 endPos = startPos + transform.forward * 10f;
        worldWaypoints.Add(endPos);
    
        currentWaypointIndex = 1;
        
        Debug.LogWarning($"EnemyMovement: Created default path for {gameObject.name} from {startPos} to {endPos}");
    }
    
    private void Update()
    {
        if (pathComplete || isMovementPaused || !isPathInitialized || 
            worldWaypoints == null || currentWaypointIndex >= worldWaypoints.Count)
            return;

        // Update speed modifiers
        UpdateSpeedModifiers();
        
        // Move towards current waypoint
        MoveTowardsWaypoint();
        
        // Rotate towards movement direction
        if (smoothRotation && currentMoveDirection != Vector3.zero)
        {
            RotateTowardsDirection();
        }
    }
    
    /// <summary>
    /// Moves the enemy towards the current waypoint
    /// </summary>
    private void MoveTowardsWaypoint()
    {

        Debug.Log($"Moving towards waypoint {currentWaypointIndex}, current pos: {transform.position}, target: {worldWaypoints[currentWaypointIndex]}");
    
        Vector3 targetPosition1 = worldWaypoints[currentWaypointIndex];
        Vector3 direction = (targetPosition1 - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition1);
    
        Debug.Log($"Distance: {distance}, Direction: {direction}, Speed: {moveSpeed}");

        // Get the current target waypoint
        Vector3 targetPosition = worldWaypoints[currentWaypointIndex];
        
        // Calculate direction and distance
        Vector3 directionToWaypoint = targetPosition - transform.position;
        float distanceToWaypoint = directionToWaypoint.magnitude;
        
        // Normalize the direction
        currentMoveDirection = directionToWaypoint.normalized;
        
        // Calculate movement distance this frame
        float effectiveSpeed = moveSpeed * speedModifier;
        float movementThisFrame = effectiveSpeed * Time.deltaTime;
        
        // Check if we're close enough to the waypoint
        if (distanceToWaypoint <= movementThisFrame)
        {
            // Snap to waypoint and move to next one
            transform.position = targetPosition;
            currentWaypointIndex++;
            
            // Check if we've reached the end of the path
            if (currentWaypointIndex >= worldWaypoints.Count)
            {
                OnReachedEnd();
                return;
            }
        }
        else
        {
            // Move towards the waypoint
            transform.position += currentMoveDirection * movementThisFrame;
        }
    }
    
    /// <summary>
    /// Rotates the enemy to face the movement direction
    /// </summary>
    private void RotateTowardsDirection()
    {
        // Create a rotation that looks in the direction of movement
        Quaternion targetRotation = Quaternion.LookRotation(currentMoveDirection);
        
        // Smoothly rotate towards the target rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
    
    /// <summary>
    /// Updates active speed modifiers and calculates the current modifier value
    /// </summary>
    private void UpdateSpeedModifiers()
    {
        // List to track expired modifiers
        List<int> expiredModifiers = new List<int>();
        
        // Update remaining time for each modifier
        foreach (var modifier in activeSpeedModifiers.Values)
        {
            modifier.RemainingTime -= Time.deltaTime;
            
            // Check if modifier has expired
            if (modifier.RemainingTime <= 0)
            {
                expiredModifiers.Add(modifier.ID);
            }
        }
        
        // Remove expired modifiers
        foreach (int id in expiredModifiers)
        {
            activeSpeedModifiers.Remove(id);
        }
        
        // Recalculate speed modifier
        // For slowing effects, we want the lowest/strongest modifier
        speedModifier = 1.0f;
        foreach (var modifier in activeSpeedModifiers.Values)
        {
            speedModifier = Mathf.Min(speedModifier, modifier.Value);
        }
    }
    
    /// <summary>
    /// Called when the enemy reaches the end of the path
    /// </summary>
    private void OnReachedEnd()
    {
        pathComplete = true;
        
        // Spawn effect if available
        if (reachEndEffectPrefab != null)
        {
            Instantiate(reachEndEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Get the EnemyController to handle end-of-path logic
        EnemyController controller = GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.ReachedEnd();
        }
        else
        {
            // If no controller exists, just destroy the object
            Destroy(gameObject, 0.1f);
        }
    }
    
    /// <summary>
    /// Adds a speed modifier to the enemy (for slowing effects)
    /// </summary>
    /// <param name="modifierID">Unique ID for this modifier</param>
    /// <param name="value">Speed multiplier (0-1 for slowing, >1 for speeding up)</param>
    /// <param name="duration">How long the modifier lasts in seconds</param>
    public void AddSpeedModifier(int modifierID, float value, float duration)
    {
        // Create new modifier
        SpeedModifier newModifier = new SpeedModifier(modifierID, value, duration);
        
        // Add or replace existing modifier with same ID
        activeSpeedModifiers[modifierID] = newModifier;
    }
    
    /// <summary>
    /// Removes a speed modifier by ID
    /// </summary>
    public void RemoveSpeedModifier(int modifierID)
    {
        if (activeSpeedModifiers.ContainsKey(modifierID))
        {
            activeSpeedModifiers.Remove(modifierID);
        }
    }
    
    /// <summary>
    /// Pauses or resumes enemy movement
    /// </summary>
    public void SetMovementPaused(bool isPaused)
    {
        isMovementPaused = isPaused;
    }
    
    /// <summary>
    /// Recalculates the path (called when the path changes)
    /// </summary>
    public void RecalculatePath()
    {
        // Only recalculate if we haven't completed the path yet and path is initialized
        if (!pathComplete && isPathInitialized && pathfindingManager != null)
        {
            // Store current progress ratio along path segment
            Vector3 currentPos = transform.position;
            
            // Get updated path
            pathWaypoints = pathfindingManager.GetCurrentPath();
            
            if (pathWaypoints == null || pathWaypoints.Count < 2)
            {
                Debug.LogWarning("EnemyMovement: Invalid path received during recalculation!");
                return;
            }
            
            // Convert to world positions
            worldWaypoints = new List<Vector3>();
            foreach (Vector2Int gridPos in pathWaypoints)
            {
                Vector3 worldPos = gridManager.GetWorldPosition(gridPos);
                worldPos.y += transform.position.y;
                worldWaypoints.Add(worldPos);
            }
            
            // Find the closest point on the new path to continue from
            FindClosestPathPosition();
        }
    }
    
    /// <summary>
    /// Finds the closest position on the path to continue from after recalculation
    /// </summary>
    private void FindClosestPathPosition()
    {
        Vector3 currentPos = transform.position;
        float closestDistance = float.MaxValue;
        int closestIndex = 0;
        
        // Find the closest waypoint to current position
        for (int i = 0; i < worldWaypoints.Count - 1; i++)
        {
            Vector3 pointOnLine = GetClosestPointOnLine(worldWaypoints[i], worldWaypoints[i + 1], currentPos);
            float distance = Vector3.Distance(currentPos, pointOnLine);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        // Set the next waypoint index
        currentWaypointIndex = closestIndex + 1;
        
        // Make sure we don't go out of bounds
        if (currentWaypointIndex >= worldWaypoints.Count)
        {
            currentWaypointIndex = worldWaypoints.Count - 1;
        }
    }
    
    /// <summary>
    /// Gets the closest point on a line segment to a given point
    /// </summary>
    private Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        lineDirection.Normalize();
        
        Vector3 pointToLineStart = point - lineStart;
        float projectionLength = Vector3.Dot(pointToLineStart, lineDirection);
        
        // Clamp projection to line segment
        projectionLength = Mathf.Clamp(projectionLength, 0f, lineLength);
        
        return lineStart + lineDirection * projectionLength;
    }
    
    /// <summary>
    /// Gets the current movement speed (for UI display, etc.)
    /// </summary>
    public float GetCurrentSpeed()
    {
        return moveSpeed * speedModifier;
    }
    
    /// <summary>
    /// Gets the base movement speed
    /// </summary>
    public float GetBaseSpeed()
    {
        return moveSpeed;
    }
    
    /// <summary>
    /// Sets the base movement speed
    /// </summary>
    public void SetBaseSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
    }
    

}