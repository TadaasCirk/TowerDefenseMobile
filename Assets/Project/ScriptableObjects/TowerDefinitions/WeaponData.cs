using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// ScriptableObject defining weapon properties for towers.
    /// Allows creating different weapon types that can be assigned to towers.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Tower Defense/Weapon Data", order = 2)]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Properties")]
        [Tooltip("Unique identifier for this weapon type")]
        public string weaponID;
        
        [Tooltip("Display name of the weapon")]
        public string displayName;
        
        [Tooltip("Description of the weapon's function")]
        [TextArea(3, 5)]
        public string description;
        
        [Header("Projectile Properties")]
        [Tooltip("Prefab for the projectile")]
        public GameObject projectilePrefab;
        
        [Tooltip("ID for the projectile pool")]
        public string projectilePoolID = "standard";
        
        [Tooltip("Base speed of projectiles")]
        public float projectileSpeed = 10f;
        
        [Tooltip("Should projectiles home in on targets?")]
        public bool useHoming = false;
        
        [Tooltip("Homing strength (only if homing is enabled)")]
        public float homingStrength = 5f;
        
        [Tooltip("Does this weapon deal area damage?")]
        public bool hasAreaDamage = false;
        
        [Tooltip("Radius of area damage (only if area damage is enabled)")]
        public float areaDamageRadius = 1.5f;
        
        [Tooltip("Type of damage dealt by this weapon")]
        public DamageType damageType = DamageType.Normal;
        
        [Header("Effects")]
        [Tooltip("Particle effect played when firing")]
        public GameObject muzzleEffectPrefab;
        
        [Tooltip("Audio clip played when firing")]
        public AudioClip firingSound;
        
        [Header("Level Scaling")]
        [Tooltip("How projectile speed scales with level")]
        public float[] speedLevelMultipliers = new float[2] { 1.1f, 1.2f };
        
        [Tooltip("How area damage radius scales with level (if applicable)")]
        public float[] radiusLevelMultipliers = new float[2] { 1.1f, 1.2f };
        
        /// <summary>
        /// Gets the speed multiplier for the specified level
        /// </summary>
        public float GetSpeedMultiplier(int level)
        {
            if (level <= 1) return 1f;
            
            int index = level - 2;
            if (index >= 0 && index < speedLevelMultipliers.Length)
            {
                return speedLevelMultipliers[index];
            }
            
            // Default fallback
            float lastMultiplier = speedLevelMultipliers.Length > 0 
                ? speedLevelMultipliers[speedLevelMultipliers.Length - 1] 
                : 1f;
                
            return lastMultiplier;
        }
        
        /// <summary>
        /// Gets the area radius multiplier for the specified level
        /// </summary>
        public float GetRadiusMultiplier(int level)
        {
            if (level <= 1) return 1f;
            if (!hasAreaDamage) return 0f;
            
            int index = level - 2;
            if (index >= 0 && index < radiusLevelMultipliers.Length)
            {
                return radiusLevelMultipliers[index];
            }
            
            // Default fallback
            float lastMultiplier = radiusLevelMultipliers.Length > 0 
                ? radiusLevelMultipliers[radiusLevelMultipliers.Length - 1] 
                : 1f;
                
            return lastMultiplier;
        }
    }
}