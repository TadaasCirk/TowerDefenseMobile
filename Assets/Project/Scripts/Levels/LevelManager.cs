using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int initialPlayerHealth = 10;
    [SerializeField] private int initialGold = 100;
    [SerializeField] private int totalWaves = 10;
    
    [Header("UI References")]
    [SerializeField] private Text healthText;
    [SerializeField] private Text goldText;
    [SerializeField] private Text waveText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;
    
    private int currentHealth;
    private int currentGold;
    private int currentWave = 0;
    private bool isGameOver = false;
    
    private void Start()
    {
        InitializeLevel();
        SetupUI();
    }
    
    private void InitializeLevel()
    {
        currentHealth = initialPlayerHealth;
        currentGold = initialGold;
        
        // Reset wave counter
        currentWave = 0;
    }
    
    private void SetupUI()
    {
        // Set initial UI values
        UpdateHealthUI();
        UpdateGoldUI();
        UpdateWaveUI();
        
        // Setup button listeners
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);
            
        // Ensure pause panel is hidden
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }
    
    public void StartNextWave()
    {
        if (currentWave < totalWaves && !isGameOver)
        {
            currentWave++;
            UpdateWaveUI();
            
            // TODO: Implement wave spawning logic
            Debug.Log($"Starting Wave {currentWave}");
        }
        else if (currentWave >= totalWaves)
        {
            // Level complete
            LevelComplete();
        }
    }
    
    public void TakeDamage(int damage = 1)
    {
        if (isGameOver) return;
        
        currentHealth -= damage;
        UpdateHealthUI();
        
        if (currentHealth <= 0)
        {
            GameOver();
        }
    }
    
    public bool TrySpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateGoldUI();
            return true;
        }
        return false;
    }
    
    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldUI();
    }
    
    private void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = $"HP: {currentHealth}";
    }
    
    private void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"Gold: {currentGold}";
    }
    
    private void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = $"Wave: {currentWave}/{totalWaves}";
    }
    
    private void TogglePause()
    {
        if (pausePanel != null)
        {
            bool isPaused = !pausePanel.activeSelf;
            pausePanel.SetActive(isPaused);
            Time.timeScale = isPaused ? 0f : 1f;
        }
    }
    
    private void GameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over");
        // TODO: Show game over UI
    }
    
    private void LevelComplete()
    {
        Debug.Log("Level Complete!");
        // TODO: Show victory UI
    }
    
    public void ReturnToMainMenu()
    {
        // Make sure to reset timeScale if we're paused
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }
    
    public void RestartLevel()
    {
        // Make sure to reset timeScale if we're paused
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}