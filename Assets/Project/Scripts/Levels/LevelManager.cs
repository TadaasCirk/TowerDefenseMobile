using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using TowerDefense.Core;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameManager gameManager;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    
    // Level state tracking - references values from GameManager
    private int currentWave = 0;
    private int totalWaves = 10;
    
    private void Start()
    {
        // Find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("LevelManager: Could not find GameManager!");
            }
        }
        
        // Find EnemySpawner if not assigned
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
            if (enemySpawner == null)
            {
                Debug.LogWarning("LevelManager: Could not find EnemySpawner!");
            }
            else 
            {
                totalWaves = enemySpawner.GetTotalWavesCount();
            }
        }
        
        SetupUI();
        
        // Subscribe to GameManager events
        if (gameManager != null)
        {
            gameManager.OnHealthChanged += UpdateHealthUI;
            gameManager.OnGoldChanged += UpdateGoldUI;
            gameManager.OnGameOver += HandleGameOver;
            gameManager.OnLevelComplete += HandleLevelComplete;
            gameManager.OnGamePaused += HandleGamePaused;
            gameManager.OnGameResumed += HandleGameResumed;
            
            // Initialize UI with current values
            UpdateHealthUI(gameManager.GetHealth());
            UpdateGoldUI(gameManager.GetGold());
        }
        
        // Subscribe to EnemySpawner events
        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStart += OnWaveStarted;
            enemySpawner.OnWaveComplete += OnWaveCompleted;
            enemySpawner.OnAllWavesComplete += OnAllWavesCompleted;
        }
        
        // Update the wave UI
        UpdateWaveUI();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from GameManager events
        if (gameManager != null)
        {
            gameManager.OnHealthChanged -= UpdateHealthUI;
            gameManager.OnGoldChanged -= UpdateGoldUI;
            gameManager.OnGameOver -= HandleGameOver;
            gameManager.OnLevelComplete -= HandleLevelComplete;
            gameManager.OnGamePaused -= HandleGamePaused;
            gameManager.OnGameResumed -= HandleGameResumed;
        }
        
        // Unsubscribe from EnemySpawner events
        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStart -= OnWaveStarted;
            enemySpawner.OnWaveComplete -= OnWaveCompleted;
            enemySpawner.OnAllWavesComplete -= OnAllWavesCompleted;
        }
        
        // Clean up button listeners
        if (pauseButton != null)
            pauseButton.onClick.RemoveAllListeners();
        if (resumeButton != null)
            resumeButton.onClick.RemoveAllListeners();
        if (restartButton != null)
            restartButton.onClick.RemoveAllListeners();
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveAllListeners();
    }
    
    private void SetupUI()
    {
        // Set up button listeners
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);
        
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        
        // Ensure pause panel is hidden
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }
    
    // Event handlers for GameManager events
    
    private void UpdateHealthUI(int health)
    {
        if (healthText != null)
            healthText.text = $"HP: {health}";
    }
    
    private void UpdateGoldUI(int gold)
    {
        if (goldText != null)
            goldText.text = $"Gold: {gold}";
    }
    
    private void HandleGameOver()
    {
        Debug.Log("LevelManager: Game Over");
        // Could show game over UI here
    }
    
    private void HandleLevelComplete()
    {
        Debug.Log("LevelManager: Level Complete");
        // Could show victory UI here
    }
    
    private void HandleGamePaused()
    {
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }
    
    private void HandleGameResumed()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }
    
    // Event handlers for EnemySpawner events
    
    private void OnWaveStarted(int waveNumber)
    {
        currentWave = waveNumber;
        UpdateWaveUI();
    }
    
    private void OnWaveCompleted(int waveNumber)
    {
        Debug.Log($"Wave {waveNumber} completed!");
        // Could provide wave completion rewards here
    }
    
    private void OnAllWavesCompleted(int totalWaves)
    {
        if (gameManager != null)
        {
            gameManager.LevelComplete();
        }
    }
    
    // UI Updates
    
    private void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = $"Wave: {currentWave}/{totalWaves}";
    }
    
    // Button handlers
    
    public void StartNextWave()
    {
        if (enemySpawner != null)
        {
            enemySpawner.StartNextWave();
        }
    }
    
    public void TogglePause()
    {
        if (gameManager != null)
        {
            gameManager.TogglePause();
        }
    }
    
    public void ResumeGame()
    {
        if (gameManager != null)
        {
            gameManager.SetGamePaused(false);
        }
    }
    
    public void RestartLevel()
    {
        if (gameManager != null)
        {
            gameManager.RestartLevel();
        }
    }
    
    public void ReturnToMainMenu()
    {
        // Ensure time scale is reset
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }
    
    // Game speed controls
    
    public void SetGameSpeed(float speed)
    {
        if (gameManager != null)
        {
            gameManager.SetGameSpeed(speed);
        }
    }
    
    public void CycleGameSpeed()
    {
        if (gameManager != null)
        {
            gameManager.CycleGameSpeed();
        }
    }
    
    // Debug methods
    
    public void AddDebugGold(int amount = 100)
    {
        if (gameManager != null)
        {
            gameManager.AddGold(amount);
        }
    }
    
    public void SetDebugHealth(int health = 10)
    {
        if (gameManager != null)
        {
            gameManager.SetHealth(health);
        }
    }
}