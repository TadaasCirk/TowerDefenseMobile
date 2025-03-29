using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    
    [Header("Path Settings")]
    [Tooltip("Y position offset from the path (height above the grid)")]
    [SerializeField] private float heightOffset = 0.5f;
    
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
        // Find managers if they exist in the scene
        gridManager = FindObjectOfType<GridManager>();
        pathfindingManager = FindObjectOfType<PathfindingManager>();
        
        if (gridManager == null || pathfindingManager == null)
        {
            Debug.LogError("EnemyMovement: Could not find GridManager or PathfindingManager!");
        }
    }
    
    private void Start()
    {
        // Add debug logging to track initialization
        Debug.Log("EnemyMovement Start - Initializing path");
    
        // Get the current path from the pathfinding manager
        InitializePath();
    
        // Add a fallback if initialization fails
        if (pathWaypoints == null || pathWaypoints.Count == 0)
        {
            Debug.LogWarning("Path initialization failed - creating default path");
            CreateDefaultPath();
        }
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
    }
    
    private void Update()
    {
        if (pathComplete || isMovementPaused || pathWaypoints == null || currentWaypointIndex >= worldWaypoints.Count)
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
    /// Initializes the enemy's path based on the current pathfinding data
    /// </summary>
    private void InitializePath()
    {
        if (pathfindingManager == null)
            return;
        
        // Get the path from the pathfinding manager
        pathWaypoints = pathfindingManager.GetCurrentPath();
        
        if (pathWaypoints == null || pathWaypoints.Count < 2)
        {
            Debug.LogWarning("EnemyMovement: Invalid path received!");
            return;
        }
        
        // Convert grid positions to world positions
        worldWaypoints = new List<Vector3>();
        foreach (Vector2Int gridPos in pathWaypoints)
        {
            Vector3 worldPos = gridManager.GetWorldPosition(gridPos);
            worldPos.y += heightOffset; // Add height offset
            worldWaypoints.Add(worldPos);
        }
        
        // Set initial position to first waypoint
        transform.position = worldWaypoints[0];
        currentWaypointIndex = 1; // Start moving toward the second waypoint
        
        // If there's a next waypoint, set initial rotation
        if (currentWaypointIndex < worldWaypoints.Count)
        {
            Vector3 direction = worldWaypoints[currentWaypointIndex] - transform.position;
            direction.y = 0; // Keep rotation on the horizontal plane
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    
    /// <summary>
    /// Moves the enemy towards the current waypoint
    /// </summary>
    private void MoveTowardsWaypoint()
    {
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
        
        // Notify the game manager that an enemy reached the end
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.EnemyReachedEnd(gameObject);
        }
        
        // Spawn effect if available
        if (reachEndEffectPrefab != null)
        {
            Instantiate(reachEndEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Destroy the enemy object
        Destroy(gameObject, 0.1f);
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
        // Only recalculate if we haven't completed the path yet
        if (!pathComplete)
        {
            // Store current progress ratio along path segment
            Vector3 currentPos = transform.position;
            Vector3 currentTarget = worldWaypoints[currentWaypointIndex];
            Vector3 previousWaypoint = worldWaypoints[currentWaypointIndex - 1];
            
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
                worldPos.y += heightOffset;
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
    
    /// <summary>
    /// Debugging gizmos to show the path
    /// </summary>
    private void OnDrawGizmos()
    {
        if (worldWaypoints == null || worldWaypoints.Count < 2)
            return;
        
        Gizmos.color = Color.cyan;
        
        // Draw lines between waypoints
        for (int i = 0; i < worldWaypoints.Count - 1; i++)
        {
            Gizmos.DrawLine(worldWaypoints[i], worldWaypoints[i + 1]);
        }
        
        // Draw spheres at waypoints
        for (int i = 0; i < worldWaypoints.Count; i++)
        {
            Gizmos.DrawSphere(worldWaypoints[i], 0.2f);
        }
        
        // Highlight current target waypoint
        if (currentWaypointIndex < worldWaypoints.Count)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(worldWaypoints[currentWaypointIndex], 0.3f);
        }
    }
}