using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Central manager class for the tower defense game.
    /// Handles player stats, resources, game state, and core gameplay events.
    /// </summary>
    public class GameManager : SingletonBehaviour<GameManager>
    {
        [Header("Player Settings")]
        [Tooltip("Starting player health")]
        [SerializeField] private int playerHealth = 10;
        
        [Tooltip("Starting gold amount")]
        [SerializeField] private int startingGold = 100;

        [Header("Game State")]
        [Tooltip("Is the game paused?")]
        [SerializeField] private bool isPaused = false;
        
        [Tooltip("Game speed multiplier")]
        [SerializeField] private float gameSpeed = 1.0f;
        
        [Tooltip("Available game speed settings")]
        [SerializeField] private float[] speedOptions = { 1.0f, 1.5f, 2.0f };

        // Game state tracking
        private int currentHealth;
        private int currentGold;
        private int currentLevel = 1;
        private int playerExperience = 0;
        private bool isGameOver = false;
        private bool isLevelComplete = false;

        // Events
        public event Action<int> OnHealthChanged;
        public event Action<int> OnGoldChanged;
        public event Action<int> OnExperienceGained;
        public event Action<float> OnGameSpeedChanged;
        public event Action OnGameOver;
        public event Action OnLevelComplete;
        public event Action OnGamePaused;
        public event Action OnGameResumed;

        /// <summary>
        /// Initialization method called when the singleton instance is created
        /// Called by SingletonBehaviour<T> during Awake
        /// </summary>
        protected override void OnSingletonAwake()
        {
            // Initialize player stats
            currentHealth = playerHealth;
            currentGold = startingGold;
            
            // Register any additional interfaces this manager implements
            // Example: ServiceLocator.Register<IPlayerStats>(this);
            
            Debug.Log("GameManager initialized and registered with ServiceLocator");
        }

        private void Start()
        {
            // Subscribe to global events
            SubscribeToEvents();
            
            // Trigger initial events to update UI
            OnHealthChanged?.Invoke(currentHealth);
            OnGoldChanged?.Invoke(currentGold);
        }
        
        /// <summary>
        /// Subscribe to global events from GameEvents
        /// </summary>
        private void SubscribeToEvents()
        {
            GameEvents.OnEnemyReachedEnd += HandleEnemyReachedEnd;
            GameEvents.OnEnemyDefeated += HandleEnemyDefeated;
        }
        
        /// <summary>
        /// Unsubscribe from global events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnEnemyReachedEnd -= HandleEnemyReachedEnd;
            GameEvents.OnEnemyDefeated -= HandleEnemyDefeated;
        }
        
        /// <summary>
        /// Handle enemy reaching the end event
        /// </summary>
        private void HandleEnemyReachedEnd(int damage)
        {
            PlayerTakeDamage(damage);
        }
        
        /// <summary>
        /// Handle enemy defeated event
        /// </summary>
        private void HandleEnemyDefeated(int goldReward, int experienceReward)
        {
            // Add gold
            currentGold += goldReward;
            OnGoldChanged?.Invoke(currentGold);
            
            // Add experience
            playerExperience += experienceReward;
            OnExperienceGained?.Invoke(playerExperience);
        }
        
        /// <summary>
        /// Clean up when the object is destroyed
        /// </summary>
        protected override void OnDestroy()
        {
            // Unsubscribe from events
            UnsubscribeFromEvents();
            
            // Clean up any events
            OnHealthChanged = null;
            OnGoldChanged = null;
            OnExperienceGained = null;
            OnGameSpeedChanged = null;
            OnGameOver = null;
            OnLevelComplete = null;
            OnGamePaused = null;
            OnGameResumed = null;
            
            // Call base to unregister from ServiceLocator
            base.OnDestroy();
        }

        /// <summary>
        /// Handles player taking damage when enemies reach the end
        /// </summary>
        public void PlayerTakeDamage(int damage)
        {
            if (isGameOver) return;
            
            currentHealth -= damage;
            
            // Ensure health doesn't go below 0
            currentHealth = Mathf.Max(0, currentHealth);
            
            // Notify listeners
            OnHealthChanged?.Invoke(currentHealth);
            
            // Check for game over
            if (currentHealth <= 0)
            {
                GameOver();
            }
        }

        /// <summary>
        /// Attempt to spend gold and return success/failure
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (currentGold >= amount)
            {
                currentGold -= amount;
                OnGoldChanged?.Invoke(currentGold);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add gold to player's resources
        /// </summary>
        public void AddGold(int amount)
        {
            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
        }

        /// <summary>
        /// Handle game over state
        /// </summary>
        private void GameOver()
        {
            if (isGameOver) return;
            
            isGameOver = true;
            Debug.Log("Game Over!");
            
            // Notify listeners
            OnGameOver?.Invoke();
            
            // Pause the game
            SetGamePaused(true);
        }

        /// <summary>
        /// Complete the current level
        /// </summary>
        public void LevelComplete()
        {
            if (isLevelComplete) return;
            
            isLevelComplete = true;
            Debug.Log("Level Complete!");
            
            // Notify listeners
            OnLevelComplete?.Invoke();
            
            // For now, just pause the game
            SetGamePaused(true);
        }

        /// <summary>
        /// Pause or unpause the game
        /// </summary>
        public void SetGamePaused(bool paused)
        {
            isPaused = paused;
            
            // Set time scale
            Time.timeScale = paused ? 0f : gameSpeed;
            
            // Notify listeners
            if (paused)
            {
                OnGamePaused?.Invoke();
            }
            else
            {
                OnGameResumed?.Invoke();
            }
        }

        /// <summary>
        /// Toggle game pause state
        /// </summary>
        public void TogglePause()
        {
            SetGamePaused(!isPaused);
        }

        /// <summary>
        /// Set game speed multiplier
        /// </summary>
        public void SetGameSpeed(float speed)
        {
            gameSpeed = speed;
            
            // Update time scale if not paused
            if (!isPaused)
            {
                Time.timeScale = gameSpeed;
            }
            
            // Notify listeners
            OnGameSpeedChanged?.Invoke(gameSpeed);
        }

        /// <summary>
        /// Cycle to the next game speed option
        /// </summary>
        public void CycleGameSpeed()
        {
            if (speedOptions.Length == 0) return;
            
            // Find current speed index
            int currentIndex = 0;
            for (int i = 0; i < speedOptions.Length; i++)
            {
                if (Mathf.Approximately(gameSpeed, speedOptions[i]))
                {
                    currentIndex = i;
                    break;
                }
            }
            
            // Move to next speed
            int nextIndex = (currentIndex + 1) % speedOptions.Length;
            SetGameSpeed(speedOptions[nextIndex]);
        }

        /// <summary>
        /// Restart the current level
        /// </summary>
        public void RestartLevel()
        {
            // Reset game state
            isGameOver = false;
            isLevelComplete = false;
            currentHealth = playerHealth;
            currentGold = startingGold;
            
            // Unpause the game
            SetGamePaused(false);
            
            // Reload the current scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Load the next level
        /// </summary>
        public void LoadNextLevel()
        {
            // For now, just restart the level
            RestartLevel();
            
            // In a full implementation, you would load the next level scene
            // and update the currentLevel value
        }

        /// <summary>
        /// Get the current player health
        /// </summary>
        public int GetHealth()
        {
            return currentHealth;
        }

        /// <summary>
        /// Get the current gold amount
        /// </summary>
        public int GetGold()
        {
            return currentGold;
        }

        /// <summary>
        /// Check if the player can afford a purchase
        /// </summary>
        public bool CanAfford(int cost)
        {
            return currentGold >= cost;
        }

        /// <summary>
        /// Get the current game speed
        /// </summary>
        public float GetGameSpeed()
        {
            return gameSpeed;
        }

        /// <summary>
        /// Check if the game is paused
        /// </summary>
        public bool IsPaused()
        {
            return isPaused;
        }

        /// <summary>
        /// Check if the game is over
        /// </summary>
        public bool IsGameOver()
        {
            return isGameOver;
        }

        /// <summary>
        /// Get the current level number
        /// </summary>
        public int GetCurrentLevel()
        {
            return currentLevel;
        }
    }
}