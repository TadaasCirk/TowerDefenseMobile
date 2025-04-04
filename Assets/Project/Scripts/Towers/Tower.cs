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
        
        [Tooltip("ID of the projectile in the pool")]
        [SerializeField] private string projectilePoolID = "standard";
        
        [Tooltip("Should the projectile use homing?")]
        [SerializeField] private bool useHomingProjectiles = false;
        
        [Tooltip("Homing strength for projectiles")]
        [SerializeField] private float homingStrength = 5f;
        
        [Tooltip("Area damage radius (0 for single-target)")]
        [SerializeField] private float areaDamageRadius = 0f;
        
        [Tooltip("Projectile speed")]
        [SerializeField] private float projectileSpeed = 10f;

        [Header("Visuals")]
        [Tooltip("Visual representation for each upgrade level")]
        [SerializeField] private GameObject[] levelVisuals;
        
        [Tooltip("Particle effect played when attacking")]
        [SerializeField] private ParticleSystem attackEffect;
        
        [Tooltip("Audio clip played when attacking")]
        [SerializeField] private AudioClip attackSound;
        
        [Tooltip("Should the tower head rotate to face targets?")]
        [SerializeField] private bool rotateToTarget = true;
        
        [Tooltip("Transform that rotates to face the target (if null, uses this transform)")]
        [SerializeField] private Transform turretTransform;
        
        [Tooltip("How quickly the turret rotates to face targets")]
        [SerializeField] private float turretRotationSpeed = 8f;

        [Header("Economy")]
        [Tooltip("Base cost to purchase this tower")]
        [SerializeField] private int purchaseCost = 100;
        
        [Tooltip("Cost to upgrade to each level (index 0 = upgrade to level 2)")]
        [SerializeField] private int[] upgradeCosts;
        
        [Tooltip("Sell value as a percentage of total investment")]
        [SerializeField] private float sellValuePercent = 0.7f;
        
        [Header("Debug")]
        [Tooltip("Show attack range gizmo")]
        [SerializeField] private bool showRangeGizmo = true;
        
        [Tooltip("Show target indicators")]
        [SerializeField] private bool showTargetIndicators = false;

        // Reference to the tower definition
        private TowerDefinition definition;

        // Getter for the definition
        public TowerDefinition GetDefinition() => definition;   

        // Runtime properties
        protected float attackCooldown = 0f;
        protected List<EnemyController> enemiesInRange = new List<EnemyController>();
        protected EnemyController currentTarget;
        protected bool isAttacking = false;
        protected int uniqueTowerID; 
        
        // Component references
        private AudioSource audioSource;
        private SphereCollider rangeCollider;
        
        // Cached target position for rotation
        private Vector3 targetPosition;

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
            // Existing initialization code...
    
            // Initialize weapon data if available
            if (definition.weaponData != null)
            {
                projectilePrefab = definition.weaponData.projectilePrefab;
                projectilePoolID = definition.weaponData.projectilePoolID;
                projectileSpeed = definition.weaponData.projectileSpeed;
                useHomingProjectiles = definition.weaponData.useHoming;
                homingStrength = definition.weaponData.homingStrength;
                areaDamageRadius = definition.weaponData.hasAreaDamage ? definition.weaponData.areaDamageRadius : 0f;
                damageType = definition.weaponData.damageType;
        
                // Set up effects if available
                if (definition.weaponData.muzzleEffectPrefab != null && attackEffect == null)
                {
                    GameObject effectObj = Instantiate(definition.weaponData.muzzleEffectPrefab, firingPoint);
                    attackEffect = effectObj.GetComponent<ParticleSystem>();
                }
        
                if (definition.weaponData.firingSound != null && attackSound == null)
                {
                    attackSound = definition.weaponData.firingSound;
                }
            }
    
    // Store a reference to the definition for later use
    this.definition = definition;
    
    // Generate a unique ID for this tower instance
    uniqueTowerID = GetInstanceID();

    // Update visuals for initial level
    UpdateVisuals();
    
    // Configure range collider
    SetupRangeCollider();
}


        protected virtual void Awake()
        {
            // Get required components
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && attackSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
            
            // Get or create turret transform
            if (turretTransform == null)
            {
                turretTransform = transform;
            }
            
            // Get or create firing point
            if (firingPoint == null)
            {
                // Create a firing point at the top of the tower
                GameObject fp = new GameObject("FiringPoint");
                fp.transform.parent = turretTransform;
                fp.transform.localPosition = Vector3.up * 0.5f; // Slightly above tower center
                firingPoint = fp.transform;
            }
            
            // Generate unique tower ID if not initialized via definition
            if (uniqueTowerID == 0)
            {
                uniqueTowerID = GetInstanceID();
            }
            
            // Ensure the correct visual for the current level is active
            UpdateVisuals();
            
            // Setup range trigger collider
            SetupRangeCollider();
        }

        private void Start()
        {
            // Create visual debug sphere to show range
            GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSphere.transform.SetParent(transform);
            debugSphere.transform.localPosition = Vector3.zero;
            debugSphere.transform.localScale = new Vector3(attackRange * 2, attackRange * 2, attackRange * 2);
    
            // Make it semi-transparent
            Renderer renderer = debugSphere.GetComponent<Renderer>();
            Color rangeColor = new Color(0, 0.5f, 1f, 0.3f);
            renderer.material.color = rangeColor;
    
            // Remove the collider from the debug sphere so it doesn't interfere
            Destroy(debugSphere.GetComponent<Collider>());
    
            debugSphere.name = "DebugRangeSphere";
            // Initialize attack cooldown with a slight random offset
            // This prevents all towers of the same type from firing simultaneously
            attackCooldown = Random.Range(0f, 0.5f);
        }

        private void OnEnable()
        {
            // Reset attack cooldown when enabling the tower
            attackCooldown = Random.Range(0f, 0.5f);
        }

        protected virtual void Update()
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
            
            // Handle turret rotation if we have a target
            if (rotateToTarget && currentTarget != null)
            {
                RotateTurretToTarget();
            }
            
            // Clean up the enemies in range list (remove null or inactive enemies)
            CleanEnemiesInRangeList();
        }

        private void OnDrawGizmosSelected()
        {
            if (showRangeGizmo)
            {
                // Draw attack range when selected in the editor
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
                Gizmos.DrawSphere(transform.position, attackRange);
            }
            
            // Draw line to current target if debugging
            if (Application.isPlaying && showTargetIndicators && currentTarget != null)
            {
                Gizmos.color = Color.red;
                Vector3 startPos = firingPoint != null ? firingPoint.position : transform.position;
                Gizmos.DrawLine(startPos, currentTarget.transform.position);
            }
        }

        /// <summary>
        /// Set up the range collider for detecting enemies
        /// </summary>
        private void SetupRangeCollider()
        {
            // Get or add a SphereCollider component
            rangeCollider = GetComponent<SphereCollider>();
            if (rangeCollider == null)
            {
                rangeCollider = gameObject.AddComponent<SphereCollider>();
            }
            
            // Configure the collider
            rangeCollider.radius = attackRange;
            rangeCollider.isTrigger = true;
            rangeCollider.enabled = true;
        }

        /// <summary>
        /// Updates the range collider radius
        /// </summary>
        private void UpdateRangeCollider()
        {
            if (rangeCollider != null)
            {
                rangeCollider.radius = attackRange;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"Something entered trigger: {other.gameObject.name}, Layer: {other.gameObject.layer}");
            // Check if an enemy entered the range
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                Debug.Log($"Enemy entered tower range: {enemy.name}");
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
        /// Rotates the turret to face the current target
        /// </summary>
        private void RotateTurretToTarget()
        {
            if (currentTarget == null || turretTransform == null)
                return;
                
            // Get target position, ignoring Y difference
            Vector3 targetPos = currentTarget.transform.position;
            Vector3 directionToTarget = targetPos - turretTransform.position;
            directionToTarget.y = 0; // Keep rotation on XZ plane
            
            if (directionToTarget != Vector3.zero)
            {
                // Create rotation towards target
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                
                // Smoothly rotate
                turretTransform.rotation = Quaternion.Slerp(
                    turretTransform.rotation,
                    targetRotation,
                    turretRotationSpeed * Time.deltaTime
                );
            }
        }

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
                    
                // Check if we can target flying enemies (if implemented in enemy)
                // This is left as a placeholder for a future flying enemy flag implementation
                bool isFlying = false; // enemy.IsFlying;
                if (isFlying && !canTargetFlying)
                    continue;
                
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
            // Implementation depends on enemy path progress tracking
            // For now, returning the first enemy in the list
            if (targets.Count > 0)
                return targets[0];
                
            return null;
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

        protected virtual void Attack()
        {
            if (currentTarget == null || firingPoint == null)
                return;
            Debug.Log("Tower attacking!");
            // Start attack cooldown
            attackCooldown = attackRate;
    
            // Play effects
            PlayAttackEffects();
    
            // Fire projectile
            FireProjectile();
    
            // Notify listeners
            OnAttack?.Invoke(this, currentTarget);
        }

        /// <summary>
        /// Plays visual and audio effects for the attack
        /// </summary>
        private void PlayAttackEffects()
        {
            // Play particle effect if available
            if (attackEffect != null)
            {
                attackEffect.Play();
            }
            
            // Play audio if available
            if (audioSource != null && attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
        }

        /// <summary>
        /// Fires a projectile at the current target
        /// </summary>
        private void FireProjectile()
        {
            if (currentTarget == null || firingPoint == null)
                return;
                
            // Check if we should use object pooling or direct instantiation
            if (ProjectilePool.Instance != null)
            {
                // Get projectile from pool
                GameObject projectileObj = ProjectilePool.Instance.GetProjectile(projectilePoolID);
                
                if (projectileObj != null)
                {
                    // Position and initialize the projectile
                    projectileObj.transform.position = firingPoint.position;
                    
                    Projectile projectile = projectileObj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        // Initialize with target
                        projectile.Initialize(currentTarget.transform, attackDamage, damageType, uniqueTowerID);
                        
                        // Configure projectile properties
                        projectile.SetSpeed(projectileSpeed);
                        projectile.SetHoming(useHomingProjectiles, homingStrength);
                        projectile.SetAreaRadius(areaDamageRadius);
                    }
                }
            }
            else
            {
                // Direct instantiation as fallback
                if (projectilePrefab != null)
                {
                    GameObject projectileObj = Instantiate(projectilePrefab, firingPoint.position, Quaternion.identity);
                    Projectile projectile = projectileObj.GetComponent<Projectile>();
                    
                    if (projectile != null)
                    {
                        // Initialize with target
                        projectile.Initialize(currentTarget.transform, attackDamage, damageType, uniqueTowerID);
                        
                        // Configure projectile properties
                        projectile.SetSpeed(projectileSpeed);
                        projectile.SetHoming(useHomingProjectiles, homingStrength);
                        projectile.SetAreaRadius(areaDamageRadius);
                    }
                }
                else
                {
                    Debug.LogWarning("Tower: No projectile prefab assigned and no pool available");
                }
            }
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
            GameManager gameManager = ServiceLocator.Get<GameManager>();
            if (gameManager != null && !gameManager.SpendGold(upgradeCost))
            {
                return false;
            }
            
            // Perform upgrade
            level++;
            
            // Update tower stats based on level
            UpdateStatsForLevel();
            
            // Update visual representation
            UpdateVisuals();
            
            // Update range collider
            UpdateRangeCollider();
            
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
            GameManager gameManager = ServiceLocator.Get<GameManager>();
            if (gameManager != null)
            {
                gameManager.AddGold(sellValue);
            }
            
            // Destroy the tower
            Destroy(gameObject);
            
            return sellValue;
        }

        /// <summary>
        /// Updates tower stats based on the current level
        /// </summary>
        private void UpdateStatsForLevel()
        {
            if (definition != null)
            {
                // Use multipliers from definition
                attackDamage = definition.attackDamage * definition.GetDamageMultiplier(level);
                attackRange = definition.attackRange * definition.GetRangeMultiplier(level);
                attackRate = definition.attackRate * definition.GetAttackRateMultiplier(level);
            }
            else
            {
                // This is a simple linear scaling, can be overridden in derived classes
                float levelMultiplier = 1f + (level - 1) * 0.25f;
                
                // Scale attack properties
                attackDamage = GetBaseDamage() * levelMultiplier;
                attackRange = GetBaseRange() * Mathf.Sqrt(levelMultiplier);
                attackRate = GetBaseAttackRate() / Mathf.Sqrt(levelMultiplier);
            }
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
            
            // Use definition if available
            if (definition != null)
            {
                return definition.GetUpgradeCost(level);
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
            
            // Re-acquire target with new method
            AcquireTarget();
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