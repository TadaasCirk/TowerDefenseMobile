using System;
using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// A static event manager that decouples event publishers from subscribers
    /// Provides game-wide events for components to communicate without direct references
    /// </summary>
    public static class GameEvents
    {
        // Enemy-related events
        public static event Action<int> OnEnemyReachedEnd;
        public static event Action<int, int> OnEnemyDefeated;
        
        // Player-related events
        public static event Action<int> OnPlayerHealthChanged;
        public static event Action<int> OnPlayerGoldChanged;
        public static event Action<int> OnPlayerExperienceGained;
        
        // Game state events
        public static event Action OnGameOver;
        public static event Action OnLevelComplete;
        
        // Wave-related events
        public static event Action<int> OnWaveStart;
        public static event Action<int> OnWaveComplete;
        public static event Action<int> OnAllWavesComplete;
        
        /// <summary>
        /// Triggers when an enemy reaches the endpoint
        /// </summary>
        /// <param name="damage">Damage to player</param>
        public static void EnemyReachedEnd(int damage)
        {
            Debug.Log($"GameEvents: Enemy reached end with damage {damage}");
            OnEnemyReachedEnd?.Invoke(damage);
        }
        
        /// <summary>
        /// Triggers when an enemy is defeated
        /// </summary>
        /// <param name="goldReward">Gold reward</param>
        /// <param name="experienceReward">Experience reward</param>
        public static void EnemyDefeated(int goldReward, int experienceReward)
        {
            OnEnemyDefeated?.Invoke(goldReward, experienceReward);
        }
        
        /// <summary>
        /// Clears all event subscriptions
        /// Call this when unloading a scene or to prevent memory leaks
        /// </summary>
        public static void ClearAllEvents()
        {
            OnEnemyReachedEnd = null;
            OnEnemyDefeated = null;
            OnPlayerHealthChanged = null;
            OnPlayerGoldChanged = null;
            OnPlayerExperienceGained = null;
            OnGameOver = null;
            OnLevelComplete = null;
            OnWaveStart = null;
            OnWaveComplete = null;
            OnAllWavesComplete = null;
        }
    }
}