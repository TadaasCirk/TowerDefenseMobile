using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// Machine Gun Tower: Fast firing tower with low damage per shot.
    /// Specializes in high fire rate with modest range and damage.
    /// </summary>
    public class MachineGunTower : Tower
    {
        [Header("Machine Gun Specific")]
        [SerializeField] private float spinUpTime = 0.5f;
        [SerializeField] private float maxFireRateMultiplier = 1.5f;
        [SerializeField] private float currentFireRateMultiplier = 1.0f;
        [SerializeField] private Transform barrelRotationTransform;
        [SerializeField] private float barrelRotationSpeed = 720f; // degrees per second
        
        private float timeSinceFiring = 0f;
        private bool isSpunUp = false;
        
        // Now we can properly override
        protected override void Update()
        {
            base.Update();
            
            // Handle spin up/down
            if (currentTarget != null)
            {
                timeSinceFiring = 0f;
                
                // Spin up
                if (!isSpunUp)
                {
                    currentFireRateMultiplier = Mathf.MoveTowards(
                        currentFireRateMultiplier, 
                        maxFireRateMultiplier, 
                        Time.deltaTime / spinUpTime);
                    
                    if (currentFireRateMultiplier >= maxFireRateMultiplier)
                    {
                        isSpunUp = true;
                    }
                }
            }
            else
            {
                timeSinceFiring += Time.deltaTime;
                
                // Spin down after not firing for a while
                if (timeSinceFiring > spinUpTime && isSpunUp)
                {
                    currentFireRateMultiplier = Mathf.MoveTowards(
                        currentFireRateMultiplier, 
                        1.0f, 
                        Time.deltaTime / (spinUpTime * 0.5f));
                    
                    if (currentFireRateMultiplier <= 1.0f)
                    {
                        isSpunUp = false;
                    }
                }
            }
            
            // Rotate barrel
            if (barrelRotationTransform != null)
            {
                float rotationAmount = barrelRotationSpeed * currentFireRateMultiplier * Time.deltaTime;
                barrelRotationTransform.Rotate(Vector3.forward, rotationAmount);
            }
        }
        
        /// <summary>
        /// Override to provide machine gun-specific attack rate
        /// </summary>
        protected override float GetBaseAttackRate()
        {
            // Return a faster attack rate when spun up
            return base.GetBaseAttackRate() / currentFireRateMultiplier;
        }
    }
}