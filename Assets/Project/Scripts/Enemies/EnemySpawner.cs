using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TowerDefense.Core;

/// <summary>
/// Manages spawning of enemies in waves for the tower defense game.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint; // Can be null if using pathfinding entry
    [SerializeField] private PathfindingManager pathfindingManager;
    [SerializeField] private bool usePathEntryPoint = true;
    
    [Header("Wave Settings")]
    [Tooltip("List of waves to spawn")]
    [SerializeField] private List<WaveData> waves = new List<WaveData>();
    
    [Tooltip("Time between waves in seconds")]
    [SerializeField] private float timeBetweenWaves = 10f;
    
    [Tooltip("Should waves automatically start?")]
    [SerializeField] private bool autoStartWaves = true;
    
    [Tooltip("Time before first wave starts")]
    [SerializeField] private float initialDelay = 5f;
    
    [Header("Debug")]
    [Tooltip("Show debug information")]
    [SerializeField] private bool showDebug = true;
    
    // In your EnemySpawner script
    [System.Serializable]
    public class WaveData
    {
        public string waveName = "Wave";
        public float initialDelay = 2f;
        public List<EnemyGroupData> enemyGroups = new List<EnemyGroupData>();
    }

    [System.Serializable]
    public class EnemyGroupData
    {
        public GameObject enemyPrefab;
        public int count = 5;
        public float timeBetweenSpawns = 0.5f;
        public float delayBeforeGroup = 0f;
        public float difficultyMultiplier = 1f;
    }


    // Dependencies
    private GridManager gridManager;
    private GameManager gameManager;
    
    // Wave state
    private int currentWaveIndex = -1;
    private WaveData currentWave;
    private int enemiesRemainingInWave = 0;
    private int totalEnemiesInCurrentWave = 0;
    private int enemiesSpawnedInCurrentWave = 0;
    private bool isSpawningWave = false;
    private float waveStartTime = 0f;
    private float nextWaveStartTime = 0f;
    
    // Enemy tracking
    private List<EnemyController> activeEnemies = new List<EnemyController>();
    private Dictionary<string, GameObject> enemyPrefabCache = new Dictionary<string, GameObject>();
    
    // Events
    public event Action<int> OnWaveStart;
    public event Action<int> OnWaveComplete;
    public event Action<int> OnAllWavesComplete;
    public event Action<float> OnWaveProgressUpdate; // 0-1 progress value
    public event Action<float> OnNextWaveCountdown; // Seconds until next wave
    
    private void Awake()
    {
        // We'll resolve dependencies in Start
    }
    
    private void Start()
    {

        if (pathfindingManager == null)
            pathfindingManager = FindObjectOfType<PathfindingManager>();
            
        // Set up spawn location based on entry point
        if (usePathEntryPoint && pathfindingManager != null && pathfindingManager.entryPoint != null)
        {
            spawnPoint = pathfindingManager.entryPoint;
        }
        else if (spawnPoint == null)
        {
            spawnPoint = transform;
            Debug.LogWarning("No spawn point specified, using EnemySpawner position.");
        }
        
        // Initialize next wave time
        if (autoStartWaves)
        {
            nextWaveStartTime = Time.time + initialDelay;
        }
    }
    
    /// <summary>
    /// Resolves dependencies using ServiceLocator
    /// </summary>
    private void ResolveDependencies()
    {
        // Try to get dependencies from ServiceLocator
        if (pathfindingManager == null)
        {
            pathfindingManager = ServiceLocator.Get<PathfindingManager>(true);
            
            // Fallback to FindObjectOfType
            if (pathfindingManager == null)
            {
                pathfindingManager = FindObjectOfType<PathfindingManager>();
                if (pathfindingManager == null)
                {
                    Debug.LogWarning("EnemySpawner: Could not find PathfindingManager!");
                }
            }
        }
        
        // Get GridManager
        gridManager = ServiceLocator.Get<GridManager>(true);
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogWarning("EnemySpawner: Could not find GridManager!");
            }
        }
        
        // Get GameManager
        gameManager = ServiceLocator.Get<GameManager>(true);
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("EnemySpawner: Could not find GameManager!");
            }
        }
    }
    
    /// <summary>
    /// Sets up the spawn point, either from defined transform or from path entry
    /// </summary>
    private void SetupSpawnPoint()
    {
        if (usePathEntryPoint && pathfindingManager != null && gridManager != null)
        {
            // Get the entry point from pathfinding manager
            if (spawnPoint == null)
            {
                Vector2Int entryGridPos = pathfindingManager.GetEntryGridPosition();
                Vector3 entryWorldPos = gridManager.GetWorldPosition(entryGridPos);
                
                // Create a spawn point Transform at the entry position
                GameObject spawnPointObj = new GameObject("SpawnPoint");
                spawnPointObj.transform.position = entryWorldPos;
                spawnPointObj.transform.SetParent(transform);
                spawnPoint = spawnPointObj.transform;
            }
        }
        
        if (spawnPoint == null)
        {
            Debug.LogError("EnemySpawner: No spawn point assigned!");
            spawnPoint = transform;
        }
    }
    
    private void Update()
    {
        // Auto-start waves if enabled
        if (autoStartWaves && !isSpawningWave && currentWaveIndex < waves.Count - 1)
        {
            float timeUntilNextWave = nextWaveStartTime - Time.time;
            
            // Notify listeners about countdown
            OnNextWaveCountdown?.Invoke(timeUntilNextWave);
            
            // Start next wave when timer expires
            if (Time.time >= nextWaveStartTime)
            {
                StartNextWave();
            }
        }
        
        // Update wave progress
        if (isSpawningWave && totalEnemiesInCurrentWave > 0)
        {
            // Calculate progress based on enemies defeated
            float progress = 1f - ((float)enemiesRemainingInWave / totalEnemiesInCurrentWave);
            OnWaveProgressUpdate?.Invoke(progress);
            
            // Check if wave is complete (all enemies spawned and none active)
            if (enemiesSpawnedInCurrentWave >= totalEnemiesInCurrentWave && activeEnemies.Count == 0)
            {
                CompleteWave();
            }
        }
        
        // Clean up any null references in active enemies list
        CleanupActiveEnemiesList();
    }
    
    /// <summary>
    /// Starts the next wave in sequence
    /// </summary>
    public void StartNextWave()
    {
        if (isSpawningWave)
        {
            Debug.LogWarning("EnemySpawner: Cannot start next wave while current wave is in progress");
            return;
        }
        
        currentWaveIndex++;
        
        if (currentWaveIndex >= waves.Count)
        {
            OnAllWavesComplete?.Invoke(waves.Count);
            return;
        }
        
        // Start the wave
        StartWave(currentWaveIndex);
    }
    
    /// <summary>
    /// Starts a specific wave by index
    /// </summary>
    public void StartWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= waves.Count)
        {
            Debug.LogError($"EnemySpawner: Invalid wave index: {waveIndex}");
            return;
        }
        
        if (isSpawningWave)
        {
            Debug.LogWarning("EnemySpawner: Cannot start wave while another is in progress");
            return;
        }
        
        // Set current wave
        currentWaveIndex = waveIndex;
        currentWave = waves[waveIndex];
        
        // Calculate total enemies in this wave
        totalEnemiesInCurrentWave = 0;
        foreach (var enemyGroup in currentWave.enemyGroups)
        {
            totalEnemiesInCurrentWave += enemyGroup.count;
        }
        
        enemiesRemainingInWave = totalEnemiesInCurrentWave;
        enemiesSpawnedInCurrentWave = 0;
        
        // Start wave spawning coroutine
        isSpawningWave = true;
        waveStartTime = Time.time;
        
        // Notify wave start
        OnWaveStart?.Invoke(currentWaveIndex + 1); // +1 for display (waves start at 1)
        
        if (showDebug)
        {
            Debug.Log($"Starting Wave {currentWaveIndex + 1} with {totalEnemiesInCurrentWave} enemies");
        }
        
        // Start spawning
        StartCoroutine(SpawnWaveCoroutine());
    }
    
    /// <summary>
    /// Coroutine to spawn a wave of enemies
    /// </summary>
    private IEnumerator SpawnWaveCoroutine()
    {
        // Wait for the wave start delay
        yield return new WaitForSeconds(currentWave.initialDelay);
        
        // Spawn each group of enemies
        foreach (var enemyGroup in currentWave.enemyGroups)
        {
            // Wait for group delay
            yield return new WaitForSeconds(enemyGroup.delayBeforeGroup);
            
            // Spawn the enemies in the group
            for (int i = 0; i < enemyGroup.count; i++)
            {
                SpawnEnemy(enemyGroup.enemyPrefab, enemyGroup.difficultyMultiplier);
                enemiesSpawnedInCurrentWave++;
                Debug.Log($"enemiesSpawnedInCurrentWave {enemiesSpawnedInCurrentWave}");
                // Wait between spawns
                yield return new WaitForSeconds(enemyGroup.timeBetweenSpawns);
            }
        }
        
        // Set next wave time
        nextWaveStartTime = Time.time + timeBetweenWaves;
        
        // Note: we don't set isSpawningWave to false here
        // Wave is considered complete only when all enemies are defeated
    }
    
    /// <summary>
    /// Spawns a single enemy
    /// </summary>
    private void SpawnEnemy(GameObject enemyPrefab, float difficultyMultiplier)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Enemy prefab is null!");
            return;
        }
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition += new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0, UnityEngine.Random.Range(-0.2f, 0.2f));
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemyObj.transform.SetParent(transform);
        
        Debug.Log($"Enemy spawned at position: {spawnPosition}");
        // Get and initialize enemy controller
        EnemyController enemyController = enemyObj.GetComponent<EnemyController>();
        
        if (enemyController == null)
        {
            Debug.LogError("EnemySpawner: Enemy prefab doesn't have an EnemyController component!");
            Destroy(enemyObj);
            return;
        }
         Debug.Log("EnemySpawner: got enemyController");

        // Subscribe to enemy events
        enemyController.OnEnemyDefeated += HandleEnemyDefeated;
        enemyController.OnEnemyReachedEnd += HandleEnemyReachedEnd;
        
        // Apply difficulty multiplier (can be implemented to scale health/damage)
        // This would require adding a method to EnemyController to apply difficulty scaling
        
        // Add to active enemies list
        activeEnemies.Add(enemyController);
    }
    
    /// <summary>
    /// Handles an enemy being defeated
    /// </summary>
    private void HandleEnemyDefeated(EnemyController enemy)
    {
        // Remove from active enemies
        activeEnemies.Remove(enemy);
        enemiesRemainingInWave--;
        
        // Unsubscribe from events
        enemy.OnEnemyDefeated -= HandleEnemyDefeated;
        enemy.OnEnemyReachedEnd -= HandleEnemyReachedEnd;
    }
    
    /// <summary>
    /// Handles an enemy reaching the end
    /// </summary>
    private void HandleEnemyReachedEnd(EnemyController enemy)
    {
        // Remove from active enemies
        activeEnemies.Remove(enemy);
        enemiesRemainingInWave--;
        
        // Unsubscribe from events
        enemy.OnEnemyDefeated -= HandleEnemyDefeated;
        enemy.OnEnemyReachedEnd -= HandleEnemyReachedEnd;
    }
    
    /// <summary>
    /// Complete the current wave and prepare for the next
    /// </summary>
    private void CompleteWave()
    {
        if (!isSpawningWave)
            return;
        
        isSpawningWave = false;
        
        // Calculate wave duration
        float waveDuration = Time.time - waveStartTime;
        
        if (showDebug)
        {
            Debug.Log($"Wave {currentWaveIndex + 1} completed in {waveDuration:F2} seconds");
        }
        
        // Notify listeners
        OnWaveComplete?.Invoke(currentWaveIndex + 1);
        
        // Check if all waves are complete
        if (currentWaveIndex >= waves.Count - 1)
        {
            OnAllWavesComplete?.Invoke(waves.Count);
        }
    }
    
    /// <summary>
    /// Cleans up any null references in the active enemies list
    /// </summary>
    private void CleanupActiveEnemiesList()
    {
        activeEnemies.RemoveAll(enemy => enemy == null);
    }
    
    /// <summary>
    /// Gets the number of active enemies
    /// </summary>
    public int GetActiveEnemiesCount()
    {
        CleanupActiveEnemiesList();
        return activeEnemies.Count;
    }
    
    /// <summary>
    /// Gets the total number of waves
    /// </summary>
    public int GetTotalWavesCount()
    {
        return waves.Count;
    }
    
    /// <summary>
    /// Gets the current wave index (0-based)
    /// </summary>
    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }
    
    /// <summary>
    /// Gets the current wave number (1-based, for display)
    /// </summary>
    public int GetCurrentWaveNumber()
    {
        return currentWaveIndex + 1;
    }
    
    /// <summary>
    /// Gets the time until the next wave starts
    /// </summary>
    public float GetTimeUntilNextWave()
    {
        return Mathf.Max(0, nextWaveStartTime - Time.time);
    }
    
    /// <summary>
    /// Skips the countdown and immediately starts the next wave
    /// </summary>
    public void SkipCountdown()
    {
        if (!isSpawningWave && currentWaveIndex < waves.Count - 1)
        {
            nextWaveStartTime = Time.time;
        }
    }
    
   
    private void OnDestroy()
    {
        // Clean up event subscriptions to prevent memory leaks
        OnWaveStart = null;
        OnWaveComplete = null;
        OnAllWavesComplete = null;
        OnWaveProgressUpdate = null;
        OnNextWaveCountdown = null;
    }
}

/// <summary>
/// Data structure for a wave of enemies
/// </summary>
[System.Serializable]
public class WaveData
{
    [Tooltip("Name of the wave (for display)")]
    public string waveName = "Wave";
    
    [Tooltip("Initial delay before wave starts")]
    public float initialDelay = 2f;
    
    [Tooltip("Groups of enemies to spawn in this wave")]
    public List<EnemyGroupData> enemyGroups = new List<EnemyGroupData>();
}

/// <summary>
/// Data structure for a group of the same enemy type within a wave
/// </summary>
[System.Serializable]
public class EnemyGroupData
{
    [Tooltip("Enemy prefab to spawn")]
    public GameObject enemyPrefab;
    
    [Tooltip("Number of enemies to spawn")]
    public int count = 5;
    
    [Tooltip("Time between spawning each enemy")]
    public float timeBetweenSpawns = 1f;
    
    [Tooltip("Delay before this group starts spawning")]
    public float delayBeforeGroup = 0f;
    
    [Tooltip("Difficulty multiplier for this group (scales health/damage)")]
    public float difficultyMultiplier = 1f;
}