using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// ScriptableObject that stores configuration data for tower types.
    /// Create assets from this class to define different tower types.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTowerDefinition", menuName = "Tower Defense/Tower Definition", order = 1)]
    public class TowerDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Unique identifier for this tower type")]
        public string towerID;
        
        [Tooltip("Display name of the tower shown in UI")]
        public string displayName;
        
        [Tooltip("Description of the tower's function")]
        [TextArea(3, 5)]
        public string description;
        
        [Tooltip("Tower icon for UI elements")]
        public Sprite icon;
        
        [Tooltip("Prefab of the tower to instantiate")]
        public GameObject towerPrefab;

        [Header("Tower Stats")]
        [Tooltip("Base attack range in grid units")]
        public float attackRange = 3f;
        
        [Tooltip("Base attack rate in seconds between attacks")]
        public float attackRate = 1f;
        
        [Tooltip("Base damage per attack")]
        public float attackDamage = 10f;
        
        [Tooltip("Type of damage this tower deals")]
        public DamageType damageType = DamageType.Normal;
        
        [Tooltip("Can this tower target flying enemies?")]
        public bool canTargetFlying = false;
        
        [Tooltip("Maximum upgrade level available")]
        public int maxLevel = 3;

        [Header("Economy")]
        [Tooltip("Cost to purchase this tower")]
        public int purchaseCost = 100;
        
        [Tooltip("Cost to upgrade to each level (index 0 = level 1→2, etc.)")]
        public int[] upgradeCosts = new int[2] { 100, 200 };
        
        [Tooltip("Sell value as a percentage of total investment")]
        [Range(0.1f, 1.0f)]
        public float sellValuePercent = 0.7f;

        [Header("Level Scaling")]
        [Tooltip("Damage multiplier per level (1 = 100% base damage)")]
        public float[] damageLevelMultipliers = new float[2] { 1.5f, 2.25f };
        
        [Tooltip("Range multiplier per level (1 = 100% base range)")]
        public float[] rangeLevelMultipliers = new float[2] { 1.2f, 1.5f };
        
        [Tooltip("Attack rate multiplier per level (< 1 means faster)")]
        public float[] attackRateLevelMultipliers = new float[2] { 0.8f, 0.6f };

        [Header("Special Properties")]
        [Tooltip("Does this tower deal area damage?")]
        public bool hasAreaDamage = false;
        
        [Tooltip("Radius of area damage if applicable")]
        public float areaDamageRadius = 0f;
        
        [Tooltip("Does this tower apply a slowing effect?")]
        public bool hasSlowingEffect = false;
        
        [Tooltip("Slow percentage if applicable (0.5 = 50% slower)")]
        [Range(0f, 1f)]
        public float slowAmount = 0f;
        
        [Tooltip("Duration of slow effect in seconds")]
        public float slowDuration = 0f;
        
        [Tooltip("Does this tower have any special abilities?")]
        public bool hasSpecialAbility = false;
        
        [Tooltip("Cooldown for special ability in seconds")]
        public float specialAbilityCooldown = 0f;

        /// <summary>
        /// Gets the damage multiplier for the specified level
        /// </summary>
        public float GetDamageMultiplier(int level)
        {
            if (level <= 1) return 1f;
            
            int index = level - 2;
            if (index >= 0 && index < damageLevelMultipliers.Length)
            {
                return damageLevelMultipliers[index];
            }
            
            // Default fallback based on last known multiplier
            float lastMultiplier = damageLevelMultipliers.Length > 0 
                ? damageLevelMultipliers[damageLevelMultipliers.Length - 1] 
                : 1f;
                
            // Apply consistent scaling beyond defined multipliers
            float additionalLevels = level - 2 - damageLevelMultipliers.Length;
            return lastMultiplier * Mathf.Pow(1.25f, additionalLevels);
        }
        
        /// <summary>
        /// Gets the range multiplier for the specified level
        /// </summary>
        public float GetRangeMultiplier(int level)
        {
            if (level <= 1) return 1f;
            
            int index = level - 2;
            if (index >= 0 && index < rangeLevelMultipliers.Length)
            {
                return rangeLevelMultipliers[index];
            }
            
            // Default fallback with similar pattern to damage multiplier
            float lastMultiplier = rangeLevelMultipliers.Length > 0 
                ? rangeLevelMultipliers[rangeLevelMultipliers.Length - 1] 
                : 1f;
                
            float additionalLevels = level - 2 - rangeLevelMultipliers.Length;
            return lastMultiplier * Mathf.Pow(1.1f, additionalLevels);
        }
        
        /// <summary>
        /// Gets the attack rate multiplier for the specified level
        /// </summary>
        public float GetAttackRateMultiplier(int level)
        {
            if (level <= 1) return 1f;
            
            int index = level - 2;
            if (index >= 0 && index < attackRateLevelMultipliers.Length)
            {
                return attackRateLevelMultipliers[index];
            }
            
            // Default fallback
            float lastMultiplier = attackRateLevelMultipliers.Length > 0 
                ? attackRateLevelMultipliers[attackRateLevelMultipliers.Length - 1] 
                : 1f;
                
            float additionalLevels = level - 2 - attackRateLevelMultipliers.Length;
            return lastMultiplier * Mathf.Pow(0.9f, additionalLevels);
        }
        
        /// <summary>
        /// Get upgrade cost for a specific level
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            int index = currentLevel - 1;
            if (index >= 0 && index < upgradeCosts.Length)
            {
                return upgradeCosts[index];
            }
            
            // Default fallback if level is beyond defined costs
            if (upgradeCosts.Length > 0 && index >= upgradeCosts.Length)
            {
                // Each additional level costs more
                int lastDefinedCost = upgradeCosts[upgradeCosts.Length - 1];
                int levelBeyondDefined = index - upgradeCosts.Length + 1;
                return lastDefinedCost + (lastDefinedCost / 2 * levelBeyondDefined);
            }
            
            return 0;
        }
        
        /// <summary>
        /// Gets the total cost including purchase and all upgrades to the specified level
        /// </summary>
        public int GetTotalCost(int level)
        {
            int totalCost = purchaseCost;
            
            for (int i = 1; i < level; i++)
            {
                totalCost += GetUpgradeCost(i);
            }
            
            return totalCost;
        }
    }
}