using UnityEngine;
using System.Collections;

namespace TowerDefense.Towers
{
    /// <summary>
    /// Cannon Tower: Slow firing tower with high area damage.
    /// Specializes in splash damage with good range.
    /// </summary>
    public class CannonTower : Tower
    {
        [Header("Cannon Specific")]
        [SerializeField] private float recoilAmount = 0.3f;
        [SerializeField] private float recoilRecoverySpeed = 2f;
        [SerializeField] private Transform cannonBarrel;
        [SerializeField] private GameObject shellEjectionPrefab;
        [SerializeField] private Transform shellEjectionPoint;
        
        private Vector3 originalBarrelPosition;
        private bool isRecoiling = false;
        
        // Now we can properly override
        protected override void Awake()
        {
            base.Awake();
            
            // Store original barrel position
            if (cannonBarrel != null)
            {
                originalBarrelPosition = cannonBarrel.localPosition;
            }
        }
        
        // Now we can properly override the Attack method
        protected override void Attack()
        {
            base.Attack();
            
            // Apply recoil effect
            if (cannonBarrel != null && !isRecoiling)
            {
                StartCoroutine(RecoilEffect());
            }
            
            // Eject shell
            if (shellEjectionPrefab != null && shellEjectionPoint != null)
            {
                EjectShell();
            }
        }
        
        /// <summary>
        /// Coroutine for cannon recoil animation
        /// </summary>
        private IEnumerator RecoilEffect()
        {
            isRecoiling = true;
            
            // Apply recoil
            Vector3 recoilPosition = originalBarrelPosition - new Vector3(recoilAmount, 0, 0);
            cannonBarrel.localPosition = recoilPosition;
            
            // Recover from recoil
            float recovery = 0f;
            while (recovery < 1f)
            {
                recovery += Time.deltaTime * recoilRecoverySpeed;
                cannonBarrel.localPosition = Vector3.Lerp(recoilPosition, originalBarrelPosition, recovery);
                yield return null;
            }
            
            // Ensure barrel is back at original position
            cannonBarrel.localPosition = originalBarrelPosition;
            isRecoiling = false;
        }
        
        /// <summary>
        /// Ejects a shell casing for visual effect
        /// </summary>
        private void EjectShell()
        {
            GameObject shell = Instantiate(shellEjectionPrefab, shellEjectionPoint.position, shellEjectionPoint.rotation);
            
            // Add random force and torque for natural looking ejection
            Rigidbody shellRb = shell.GetComponent<Rigidbody>();
            if (shellRb != null)
            {
                float ejectionForce = Random.Range(2f, 4f);
                Vector3 ejectionDir = shellEjectionPoint.right + Vector3.up * 0.5f;
                shellRb.AddForce(ejectionDir.normalized * ejectionForce, ForceMode.Impulse);
                
                // Add random spin
                shellRb.AddTorque(new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * 0.5f, ForceMode.Impulse);
                
                // Destroy after a delay
                Destroy(shell, 3f);
            }
        }
        
        /// <summary>
        /// Cannon has higher base damage but slower attack rate
        /// </summary>
        protected override float GetBaseDamage()
        {
            return base.GetBaseDamage() * 2.5f;
        }
        
        /// <summary>
        /// Cannon has slower fire rate
        /// </summary>
        protected override float GetBaseAttackRate()
        {
            return base.GetBaseAttackRate() * 2.0f;
        }
    }
}