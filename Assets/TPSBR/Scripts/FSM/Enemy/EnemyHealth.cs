using UnityEngine;

namespace TPSBR
{
        [DisallowMultipleComponent]
        [RequireComponent(typeof(Health))]
        public class EnemyHealth : MonoBehaviour
        {
                public Health Health => _health;

                [SerializeField]
                [Tooltip("Reference to the underlying health component.")]
                private Health _health;

                private void Reset()
                {
                        CacheHealth();
                }

                private void Awake()
                {
                        CacheHealth();
                }

                public bool IsAlive => _health != null && _health.IsAlive;

                public void HandleDeath()
                {
                        if (_health == null)
                                return;

                        // Pseudocode: Relay death event to owning enemy controller so it can activate the death behavior.
                }

                public void HandleRespawn()
                {
                        if (_health == null)
                                return;

                        // Pseudocode: Reset health to maximum and notify state machine that the enemy can spawn again.
                }

                private void CacheHealth()
                {
                        if (_health == null)
                        {
                                _health = GetComponent<Health>();
                        }
                }
        }
}
