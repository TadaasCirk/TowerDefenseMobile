using System;
using UnityEngine;

/// <summary>
/// Controls the core functionality of an enemy in the tower defense game.
/// Manages health, damage, rewards, and other common enemy properties.
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Stats")]
    [Tooltip("Maximum health points")]
    [SerializeField] private float maxHealth = 100f;
    
    [Tooltip("Damage dealt to player when reaching the end")]
    [SerializeField] private int damageToPlayer = 1;
    
    [Tooltip("Gold reward when defeated")]
    [SerializeField] private int goldReward = 10;
    
    [Tooltip("Experience points rewarded when defeated")]
    [SerializeField] private int experienceReward = 5;
    
    [Header("Visual Feedback")]
    [Tooltip("Visual effect when taking damage")]
    [SerializeField] private GameObject hitEffectPrefab;
    
    [Tooltip("Visual effect when defeated")]
    [SerializeField] private GameObject deathEffectPrefab;
    
    [Tooltip("Health bar UI component")]
    [SerializeField] private EnemyHealthBar healthBar;
    
    [Tooltip("Flash material when taking damage")]
    [SerializeField] private Material hitFlashMaterial;
    
    [Tooltip("Duration of hit flash effect")]
    [SerializeField] private float hitFlashDuration = 0.1f;
    
    // Internal state
    private float currentHealth;
    private bool isDead = false;
    private EnemyMovement movementController;
    
    // Cached components
    private Renderer[] renderers;
    private Material[] originalMaterials;
    
    // Events
    public event Action<EnemyController> OnEnemyDefeated;
    public event Action<EnemyController> OnEnemyReachedEnd;
    
    private void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Get movement controller
        movementController = GetComponent<EnemyMovement>();
        
        // Cache renderers and materials for hit flash effect
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }
    }
    
    private void Start()
    {
        // Set up health bar
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
    }
    
    /// <summary>
    /// Takes damage from a tower or other source
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    /// <param name="damageType">Type of damage (for resistance calculations)</param>
    /// <returns>True if the enemy died from this damage</returns>
    public bool TakeDamage(float damage, DamageType damageType = DamageType.Normal)
    {
        if (isDead)
            return false;
        
        // Apply damage type modifiers (can be expanded for enemy resistances)
        float modifiedDamage = CalculateModifiedDamage(damage, damageType);
        
        // Apply damage
        currentHealth -= modifiedDamage;
        
        // Update health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
        
        // Show hit effect
        ShowHitEffect();
        
        // Check if dead
        if (currentHealth <= 0)
        {
            Die(false);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculates modified damage based on damage type and resistances
    /// </summary>
    private float CalculateModifiedDamage(float baseDamage, DamageType damageType)
    {
        // This can be expanded to implement enemy resistances or weaknesses
        switch (damageType)
        {
            case DamageType.Fire:
                // Example: Some enemies might be vulnerable to fire
                return baseDamage * 1.2f;
                
            case DamageType.Ice:
                // Example: Some enemies might be resistant to ice
                return baseDamage * 0.8f;
                
            case DamageType.Poison:
                // Example: Some enemies might be resistant to poison
                return baseDamage * 0.9f;
                
            case DamageType.Energy:
                // Example: Some enemies might be vulnerable to energy
                return baseDamage * 1.1f;
                
            case DamageType.Physical:
            case DamageType.Normal:
            default:
                return baseDamage;
        }
    }
    
    /// <summary>
    /// Shows visual feedback when taking damage
    /// </summary>
    private void ShowHitEffect()
    {
        // Spawn hit effect particle if available
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Flash materials if available
        if (hitFlashMaterial != null)
        {
            StartCoroutine(FlashMaterials());
        }
    }
    
    /// <summary>
    /// Coroutine to flash materials when taking damage
    /// </summary>
    private System.Collections.IEnumerator FlashMaterials()
    {
        // Apply flash material to all renderers
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = hitFlashMaterial;
        }
        
        // Wait for flash duration
        yield return new WaitForSeconds(hitFlashDuration);
        
        // Restore original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
    }
    
    /// <summary>
    /// Handles enemy death
    /// </summary>
    /// <param name="reachedEnd">True if the enemy reached the end, false if defeated by towers</param>
    private void Die(bool reachedEnd)
    {
        if (isDead)
            return;
        
        isDead = true;
        
        // Stop movement
        if (movementController != null)
        {
            movementController.SetMovementPaused(true);
        }
        
        if (reachedEnd)
        {
            // Enemy reached end - notify listeners
            OnEnemyReachedEnd?.Invoke(this);
            
            // Find game manager and notify
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.PlayerTakeDamage(damageToPlayer);
            }
        }
        else
        {
            // Enemy defeated - notify listeners
            OnEnemyDefeated?.Invoke(this);
            
            // Find game manager and reward player
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.EnemyDefeated(goldReward, experienceReward);
            }
            
            // Spawn death effect if available
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
        }
        
        // Destroy the game object
        Destroy(gameObject, 0.2f);
    }
    
    /// <summary>
    /// Called when the enemy reaches the end of the path
    /// </summary>
    public void ReachedEnd()
    {
        Die(true);
    }
    
    /// <summary>
    /// Applies a slowing effect to this enemy
    /// </summary>
    /// <param name="slowId">Unique ID for this slow effect</param>
    /// <param name="slowAmount">Amount to slow (0-1 where 0.5 means 50% speed)</param>
    /// <param name="duration">Duration of slow effect in seconds</param>
    public void ApplySlow(int slowId, float slowAmount, float duration)
    {
        if (movementController != null)
        {
            movementController.AddSpeedModifier(slowId, slowAmount, duration);
        }
    }
    
    /// <summary>
    /// Gets the current health percentage
    /// </summary>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    /// <summary>
    /// Gets the gold reward for defeating this enemy
    /// </summary>
    public int GetGoldReward()
    {
        return goldReward;
    }
    
    /// <summary>
    /// Gets the experience reward for defeating this enemy
    /// </summary>
    public int GetExperienceReward()
    {
        return experienceReward;
    }
}

/// <summary>
/// Types of damage that can be dealt to enemies
/// </summary>
public enum DamageType
{
    Normal,
    Physical,
    Fire,
    Ice,
    Poison,
    Energy
}