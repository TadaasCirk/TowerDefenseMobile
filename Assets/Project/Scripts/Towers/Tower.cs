using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense.Towers
{
    /// <summary>
    /// Base class for all towers in the tower defense game.
    /// Handles common tower functionality like targeting, attacking, and upgrading.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [Header("Tower Properties")]
        [Tooltip("The unique identifier for this tower type")]
        [SerializeField] private string towerID;
        
        [Tooltip("The display name of the tower")]
        [SerializeField] private string displayName;
        
        [Tooltip("The current level of the tower")]
        [SerializeField] private int level = 1;
        
        [Tooltip("The maximum level this tower can be upgraded to")]
        [SerializeField] private int maxLevel = 3;

        [Header("Attack Properties")]
        [Tooltip("Range of the tower in grid units")]
        [SerializeField] private float attackRange = 3f;
        
        [Tooltip("Seconds between attacks")]
        [SerializeField] private float attackRate = 1f;
        
        [Tooltip("Damage dealt per attack")]
        [SerializeField] private float attackDamage = 10f;
        
        [Tooltip("Type of damage this tower deals")]
        [SerializeField] private DamageType damageType = DamageType.Normal;

        [Header("Targeting")]
        [Tooltip("The method this tower uses to select targets")]
        [SerializeField] private TargetingMethod targetingMethod = TargetingMethod.First;
        
        [Tooltip("Can this tower target flying enemies?")]
        [SerializeField] private bool canTargetFlying = false;
        
        [Tooltip("The transform from which projectiles are fired")]
        [SerializeField] private Transform firingPoint;
        
        [Tooltip("The projectile prefab to instantiate")]
        [SerializeField] private GameObject projectilePrefab;

        [Header("Visuals")]
        [Tooltip("Visual representation for each upgrade level")]
        [SerializeField] private GameObject[] levelVisuals;
        
        [Tooltip("Particle effect played when attacking")]
        [SerializeField] private ParticleSystem attackEffect;
        
        [Tooltip("Audio clip played when attacking")]
        [SerializeField] private AudioClip attackSound;

        [Header("Economy")]
        [Tooltip("Base cost to purchase this tower")]
        [SerializeField] private int purchaseCost = 100;
        
        [Tooltip("Cost to upgrade to each level (index 0 = upgrade to level 2)")]
        [SerializeField] private int[] upgradeCosts;
        
        [Tooltip("Sell value as a percentage of total investment")]
        [SerializeField] private float sellValuePercent = 0.7f;

        // Reference to the tower definition
        private TowerDefinition definition;

        // Getter for the definition
        public TowerDefinition GetDefinition() => definition;   

        // Runtime properties
        private float attackCooldown = 0f;
        private List<EnemyController> enemiesInRange = new List<EnemyController>();
        private EnemyController currentTarget;
        private bool isAttacking = false;
        
        // Component references
        private AudioSource audioSource;

        // Events
        public System.Action<Tower> OnTowerSold;
        public System.Action<Tower, int> OnTowerUpgraded;
        public System.Action<Tower, EnemyController> OnTargetAcquired;
        public System.Action<Tower, EnemyController> OnAttack;

        #region Unity Lifecycle

        /// <summary>
        /// Initializes the tower with a definition
        /// </summary>
        public void Initialize(TowerDefinition definition)
        {
            towerID = definition.towerID;
            displayName = definition.displayName;
            maxLevel = definition.maxLevel;
    
            // Set attack properties
            attackRange = definition.attackRange;
            attackRate = definition.attackRate;
            attackDamage = definition.attackDamage;
            damageType = definition.damageType;
            canTargetFlying = definition.canTargetFlying;
    
            // Set economy values
            purchaseCost = definition.purchaseCost;
            upgradeCosts = definition.upgradeCosts;
            sellValuePercent = definition.sellValuePercent;
    
            // Store a reference to the definition for later use
            this.definition = definition;
    
            // Update visuals for initial level
            UpdateVisuals();
        }


        private void Awake()
        {
            // Get required components
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && attackSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
            
            // Ensure the correct visual for the current level is active
            UpdateVisuals();
        }

        private void Start()
        {
            // Subscribe to events or initialize other components
        }

        private void OnEnable()
        {
            // Reset attack cooldown when enabling the tower
            attackCooldown = 0f;
        }

        private void Update()
        {
            // Handle attack cooldown
            if (attackCooldown > 0)
            {
                attackCooldown -= Time.deltaTime;
            }
            
            // Target acquisition and attack logic
            if (!isAttacking && attackCooldown <= 0)
            {
                AcquireTarget();
                
                if (currentTarget != null)
                {
                    Attack();
                }
            }
            
            // Clean up the enemies in range list (remove null or inactive enemies)
            CleanEnemiesInRangeList();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw attack range when selected in the editor
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if an enemy entered the range
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                AddEnemyToRangeList(enemy);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Check if an enemy exited the range
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                RemoveEnemyFromRangeList(enemy);
            }
        }

        #endregion

        #region Targeting

        /// <summary>
        /// Selects a target from the enemies in range based on targeting method
        /// </summary>
        private void AcquireTarget()
        {
            if (enemiesInRange.Count == 0)
            {
                currentTarget = null;
                return;
            }
            
            // Filter valid targets (e.g., flying enemies if can't target flying)
            List<EnemyController> validTargets = FilterValidTargets();
            
            if (validTargets.Count == 0)
            {
                currentTarget = null;
                return;
            }
            
            // Select target based on targeting method
            switch (targetingMethod)
            {
                case TargetingMethod.First:
                    currentTarget = GetFirstEnemy(validTargets);
                    break;
                    
                case TargetingMethod.Closest:
                    currentTarget = GetClosestEnemy(validTargets);
                    break;
                    
                case TargetingMethod.Strongest:
                    currentTarget = GetStrongestEnemy(validTargets);
                    break;
                    
                case TargetingMethod.Weakest:
                    currentTarget = GetWeakestEnemy(validTargets);
                    break;
                    
                default:
                    currentTarget = validTargets[0];
                    break;
            }
            
            // Notify about target acquisition
            if (currentTarget != null)
            {
                OnTargetAcquired?.Invoke(this, currentTarget);
            }
        }

        /// <summary>
        /// Filters the enemies in range based on targeting capabilities
        /// </summary>
        private List<EnemyController> FilterValidTargets()
        {
            List<EnemyController> validTargets = new List<EnemyController>();
            
            foreach (var enemy in enemiesInRange)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy)
                    continue;
                    
                // Check if we can target flying enemies
                /*
                bool isFlying = enemy.IsFlying;
                if (isFlying && !canTargetFlying)
                    continue;
                */
                
                // Add to valid targets
                validTargets.Add(enemy);
            }
            
            return validTargets;
        }

        /// <summary>
        /// Gets the enemy furthest along the path (first to reach the end)
        /// </summary>
        private EnemyController GetFirstEnemy(List<EnemyController> targets)
        {
            EnemyController firstEnemy = null;
            float furthestProgress = -1f;
            
            foreach (var enemy in targets)
            {
                /*
                // This requires the enemies to have a PathProgress property
                float progress = enemy.GetPathProgress();
                if (progress > furthestProgress)
                {
                    furthestProgress = progress;
                    firstEnemy = enemy;
                }
                */
                
                // For now, just return the first one
                return enemy;
            }
            
            return firstEnemy;
        }

        /// <summary>
        /// Gets the enemy closest to the tower
        /// </summary>
        private EnemyController GetClosestEnemy(List<EnemyController> targets)
        {
            EnemyController closestEnemy = null;
            float closestDistance = float.MaxValue;
            
            foreach (var enemy in targets)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
            
            return closestEnemy;
        }

        /// <summary>
        /// Gets the enemy with the highest health
        /// </summary>
        private EnemyController GetStrongestEnemy(List<EnemyController> targets)
        {
            EnemyController strongestEnemy = null;
            float highestHealth = -1f;
            
            foreach (var enemy in targets)
            {
                float health = enemy.GetHealthPercentage() * 100f; // Assuming GetHealthPercentage returns 0-1
                if (health > highestHealth)
                {
                    highestHealth = health;
                    strongestEnemy = enemy;
                }
            }
            
            return strongestEnemy;
        }

        /// <summary>
        /// Gets the enemy with the lowest health
        /// </summary>
        private EnemyController GetWeakestEnemy(List<EnemyController> targets)
        {
            EnemyController weakestEnemy = null;
            float lowestHealth = float.MaxValue;
            
            foreach (var enemy in targets)
            {
                float health = enemy.GetHealthPercentage() * 100f; // Assuming GetHealthPercentage returns 0-1
                if (health < lowestHealth)
                {
                    lowestHealth = health;
                    weakestEnemy = enemy;
                }
            }
            
            return weakestEnemy;
        }

        /// <summary>
        /// Adds an enemy to the in-range list
        /// </summary>
        private void AddEnemyToRangeList(EnemyController enemy)
        {
            if (!enemiesInRange.Contains(enemy))
            {
                enemiesInRange.Add(enemy);
            }
        }

        /// <summary>
        /// Removes an enemy from the in-range list
        /// </summary>
        private void RemoveEnemyFromRangeList(EnemyController enemy)
        {
            enemiesInRange.Remove(enemy);
            
            // If this was our target, clear it
            if (currentTarget == enemy)
            {
                currentTarget = null;
            }
        }

        /// <summary>
        /// Cleans up the enemies in range list, removing null or inactive enemies
        /// </summary>
        private void CleanEnemiesInRangeList()
        {
            enemiesInRange.RemoveAll(enemy => 
                enemy == null || !enemy.gameObject.activeInHierarchy);
                
            // If current target was removed, clear it
            if (currentTarget != null && 
                (!currentTarget.gameObject.activeInHierarchy || 
                !enemiesInRange.Contains(currentTarget)))
            {
                currentTarget = null;
            }
        }

        #endregion

        #region Combat

        /// <summary>
        /// Performs an attack on the current target
        /// </summary>
        private void Attack()
        {
            if (currentTarget == null || firingPoint == null || projectilePrefab == null)
                return;
                
            // Start attack cooldown
            attackCooldown = attackRate;
            
            // Play effects
            if (attackEffect != null)
            {
                attackEffect.Play();
            }
            
            if (audioSource != null && attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            // Spawn projectile
            SpawnProjectile();
            
            // Notify listeners
            OnAttack?.Invoke(this, currentTarget);
        }

        /// <summary>
        /// Spawns a projectile aimed at the current target
        /// </summary>
        private void SpawnProjectile()
        {
            // Instantiate projectile
            GameObject projectileObj = Instantiate(
                projectilePrefab, 
                firingPoint.position, 
                firingPoint.rotation);
                
            // Get projectile component
            /* 
            // This will be implemented when we create the Projectile class
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(currentTarget, attackDamage, damageType);
            }
            */
        }

        /// <summary>
        /// Applies area damage to all enemies in a radius
        /// </summary>
        protected virtual void ApplyAreaDamage(Vector3 center, float radius, float damage)
        {
            // This is a placeholder for towers that deal area damage
            // To be implemented in derived classes
        }

        #endregion

        #region Upgrades & Economy

        /// <summary>
        /// Upgrades the tower to the next level if possible
        /// </summary>
        /// <returns>True if upgrade was successful</returns>
        public bool Upgrade()
        {
            // Check if we're already at max level
            if (level >= maxLevel)
            {
                return false;
            }
            
            // Check if we have enough gold (would need GameManager reference)
            int upgradeCost = GetUpgradeCost();
            /*
            GameManager gameManager = ServiceLocator.Get<GameManager>();
            if (gameManager != null && !gameManager.SpendGold(upgradeCost))
            {
                return false;
            }
            */
            
            // Perform upgrade
            level++;
            
            // Update tower stats based on level
            UpdateStatsForLevel();
            
            // Update visual representation
            UpdateVisuals();
            
            // Notify listeners
            OnTowerUpgraded?.Invoke(this, level);
            
            return true;
        }

        /// <summary>
        /// Sells the tower and returns a portion of the investment
        /// </summary>
        /// <returns>The amount of gold returned</returns>
        public int Sell()
        {
            // Calculate total investment
            int totalInvestment = purchaseCost;
            for (int i = 0; i < level - 1; i++)
            {
                if (i < upgradeCosts.Length)
                {
                    totalInvestment += upgradeCosts[i];
                }
            }
            
            // Calculate sell value
            int sellValue = Mathf.RoundToInt(totalInvestment * sellValuePercent);
            
            // Notify listeners
            OnTowerSold?.Invoke(this);
            
            // Add gold to player (would need GameManager reference)
            /*
            GameManager gameManager = ServiceLocator.Get<GameManager>();
            if (gameManager != null)
            {
                gameManager.AddGold(sellValue);
            }
            */
            
            // Destroy the tower
            Destroy(gameObject);
            
            return sellValue;
        }

        /// <summary>
        /// Updates tower stats based on the current level
        /// </summary>
        private void UpdateStatsForLevel()
        {
            // This is a simple linear scaling, can be overridden in derived classes
            float levelMultiplier = 1f + (level - 1) * 0.25f;
            
            // Scale attack properties
            attackDamage = GetBaseDamage() * levelMultiplier;
            attackRange = GetBaseRange() * Mathf.Sqrt(levelMultiplier);
            attackRate = GetBaseAttackRate() / Mathf.Sqrt(levelMultiplier);
        }

        /// <summary>
        /// Updates the visual representation based on the current level
        /// </summary>
        private void UpdateVisuals()
        {
            // Disable all level visuals first
            if (levelVisuals != null)
            {
                foreach (var visual in levelVisuals)
                {
                    if (visual != null)
                    {
                        visual.SetActive(false);
                    }
                }
                
                // Enable the appropriate visual for the current level
                int visualIndex = Mathf.Clamp(level - 1, 0, levelVisuals.Length - 1);
                if (visualIndex >= 0 && visualIndex < levelVisuals.Length && levelVisuals[visualIndex] != null)
                {
                    levelVisuals[visualIndex].SetActive(true);
                }
            }
        }

        #endregion

        #region Getters and Setters

        public int GetLevel() => level;
        public int GetMaxLevel() => maxLevel;
        public float GetAttackRange() => attackRange;
        public float GetAttackRate() => attackRate;
        public float GetAttackDamage() => attackDamage;
        public DamageType GetDamageType() => damageType;
        public int GetPurchaseCost() => purchaseCost;
        public string GetDisplayName() => displayName;
        public string GetTowerID() => towerID;
        
        public int GetUpgradeCost()
        {
            int upgradeIndex = level - 1;
            if (upgradeIndex >= 0 && upgradeIndex < upgradeCosts.Length)
            {
                return upgradeCosts[upgradeIndex];
            }
            return 0;
        }
        
        public bool CanBeUpgraded() => level < maxLevel;
        
        // Base stats for upgrades
        protected virtual float GetBaseDamage() => attackDamage;
        protected virtual float GetBaseRange() => attackRange;
        protected virtual float GetBaseAttackRate() => attackRate;
        
        // Allow changing targeting method from outside
        public void SetTargetingMethod(TargetingMethod method)
        {
            targetingMethod = method;
        }
        
        public TargetingMethod GetTargetingMethod() => targetingMethod;

        #endregion
    }

    /// <summary>
    /// Enumeration of targeting methods available to towers
    /// </summary>
    public enum TargetingMethod
    {
        First,      // Target enemy furthest along the path
        Closest,    // Target closest enemy to the tower
        Strongest,  // Target enemy with the most health
        Weakest     // Target enemy with the least health
    }
}