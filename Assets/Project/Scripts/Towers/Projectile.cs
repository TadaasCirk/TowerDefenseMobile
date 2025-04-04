using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// Base class for all projectiles in the tower defense game.
    /// Handles movement, targeting, collision, and damage application.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Movement speed in units per second")]
        [SerializeField] private float speed = 10f;
        
        [Tooltip("Maximum projectile lifetime in seconds")]
        [SerializeField] private float maxLifetime = 5f;
        
        [Tooltip("Should this projectile home in on its target?")]
        [SerializeField] private bool homing = false;
        
        [Tooltip("How strongly the projectile turns toward its target (homing only)")]
        [SerializeField] private float homingStrength = 5f;
        
        [Header("Damage")]
        [Tooltip("Base damage amount")]
        [SerializeField] private float damage = 10f;
        
        [Tooltip("Type of damage this projectile deals")]
        [SerializeField] private DamageType damageType = DamageType.Normal;
        
        [Tooltip("Radius of area damage (0 for single-target)")]
        [SerializeField] private float areaRadius = 0f;
        
        [Header("Effects")]
        [Tooltip("Effect prefab spawned on impact")]
        [SerializeField] private GameObject impactEffectPrefab;
        
        [Tooltip("Audio clip played on impact")]
        [SerializeField] private AudioClip impactSound;
        
        // Runtime state
        private Transform target;
        private Vector3 targetPosition;
        private Vector3 direction;
        private float lifetimeTimer = 0f;
        private bool hasHit = false;
        private int sourceId = -1; // ID of the tower that fired this projectile
        
        // Cached components
        private Rigidbody2D rb;
        private Collider2D col;
        
        private void Awake()
        {
            // Cache components
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            
            // Set up physics
            if (rb != null)
            {
                rb.gravityScale = 0f; // No gravity for projectiles
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Better collision detection
            }
        }
        
        private void OnEnable()
        {
            // Reset state when projectile is activated from pool
            hasHit = false;
            lifetimeTimer = 0f;
        }
        
        private void Update()
        {
            if (hasHit)
                return;
                
            // Update lifetime and check for expiration
            lifetimeTimer += Time.deltaTime;
            if (lifetimeTimer >= maxLifetime)
            {
                Expire();
                return;
            }
            
            // Update targeting and movement
            UpdateMovement();
        }
        
        /// <summary>
        /// Initialize the projectile with target and damage information
        /// </summary>
        public void Initialize(Transform target, float damage, DamageType damageType, int sourceId = -1)
        {
            this.target = target;
            if (target != null)
            {
                this.targetPosition = target.position;
                this.direction = (targetPosition - transform.position).normalized;
            }
            
            this.damage = damage;
            this.damageType = damageType;
            this.sourceId = sourceId;
            
            // Orient the projectile toward its initial direction
            transform.right = direction;
            
            lifetimeTimer = 0f;
            hasHit = false;
        }
        
        /// <summary>
        /// Initialize the projectile with a direction instead of a target
        /// </summary>
        public void Initialize(Vector3 direction, float damage, DamageType damageType, int sourceId = -1)
        {
            this.target = null;
            this.direction = direction.normalized;
            this.damage = damage;
            this.damageType = damageType;
            this.sourceId = sourceId;
            
            // Orient the projectile toward its initial direction
            transform.right = direction;
            
            lifetimeTimer = 0f;
            hasHit = false;
        }
        
        /// <summary>
        /// Updates projectile movement and targeting
        /// </summary>
        private void UpdateMovement()
        {
            // If target is destroyed but we're still homing, expire the projectile
            if (homing && target == null)
            {
                Expire();
                return;
            }
            
            // Update direction for homing projectiles
            if (homing && target != null)
            {
                Vector3 targetDir = (target.position - transform.position).normalized;
                direction = Vector3.Lerp(direction, targetDir, Time.deltaTime * homingStrength);
                
                // Update orientation
                transform.right = direction;
            }
            
            // Move projectile
            if (rb != null)
            {
                rb.velocity = direction * speed;
            }
            else
            {
                transform.position += direction * speed * Time.deltaTime;
            }
        }
        
        /// <summary>
        /// Handle collision with enemy
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit)
                return;
                
            // Check if we hit an enemy
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                OnHitEnemy(enemy);
            }
        }
        
        /// <summary>
        /// Process hitting an enemy
        /// </summary>
        protected virtual void OnHitEnemy(EnemyController enemy)
        {
            // Apply direct damage to the hit enemy
            enemy.TakeDamage(damage, damageType);
            
            // Apply area damage if radius > 0
            if (areaRadius > 0f)
            {
                ApplyAreaDamage(enemy.transform.position);
            }
            
            // Play impact effects
            SpawnImpactEffect();
            
            // Mark as hit and begin deactivation
            hasHit = true;
            StartCoroutine(DeactivateAfterEffect());
        }
        
        /// <summary>
        /// Apply area damage to enemies within radius
        /// </summary>
        private void ApplyAreaDamage(Vector3 center)
        {
            // Find all enemy colliders within radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, areaRadius, LayerMask.GetMask("Enemy"));
            
            foreach (Collider2D col in colliders)
            {
                EnemyController enemy = col.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    // Skip the directly hit enemy to avoid double damage
                    if (enemy.transform.position == center)
                        continue;
                    
                    // Calculate distance-based damage falloff
                    float distance = Vector3.Distance(center, enemy.transform.position);
                    float damageMultiplier = 1f - (distance / areaRadius);
                    float areaDamage = damage * damageMultiplier;
                    
                    // Apply damage
                    enemy.TakeDamage(areaDamage, damageType);
                }
            }
        }
        
        /// <summary>
        /// Spawn impact effect and play sound
        /// </summary>
        private void SpawnImpactEffect()
        {
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            }
            
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, transform.position);
            }
        }
        
        /// <summary>
        /// Expire the projectile without hitting anything
        /// </summary>
        private void Expire()
        {
            hasHit = true;
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Deactivate after playing impact effect
        /// </summary>
        private IEnumerator DeactivateAfterEffect()
        {
            // Hide the projectile immediately
            GetComponent<Renderer>().enabled = false;
            
            // Disable collider
            if (col != null)
            {
                col.enabled = false;
            }
            
            // Stop movement
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            
            // Wait for effects to finish (you can tune this delay)
            yield return new WaitForSeconds(0.2f);
            
            // Deactivate GameObject (will be returned to pool)
            gameObject.SetActive(false);
            
            // Reset visibility and collider for next use
            GetComponent<Renderer>().enabled = true;
            if (col != null)
            {
                col.enabled = true;
            }
        }
        
        /// <summary>
        /// Set projectile speed
        /// </summary>
        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }
        
        /// <summary>
        /// Enable or disable homing behavior
        /// </summary>
        public void SetHoming(bool isHoming, float strength = 5f)
        {
            homing = isHoming;
            homingStrength = strength;
        }
        
        /// <summary>
        /// Set area damage radius
        /// </summary>
        public void SetAreaRadius(float radius)
        {
            areaRadius = radius;
        }
        
        /// <summary>
        /// Get the source ID of the tower that fired this projectile
        /// </summary>
        public int GetSourceId()
        {
            return sourceId;
        }
        
        /// <summary>
        /// Draw gizmos for area damage radius
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (areaRadius > 0)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawSphere(transform.position, areaRadius);
            }
        }
    }
}