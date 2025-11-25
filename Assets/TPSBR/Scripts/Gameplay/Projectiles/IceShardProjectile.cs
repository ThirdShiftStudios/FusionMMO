using Fusion;
using UnityEngine;
using TPSBR.Abilities;

namespace TPSBR
{
        public class IceShardProjectile : KinematicProjectile
        {
                [SerializeField]
                private IceShardAbilityDefinition _abilityDefinition;

                public void ConfigureDamage(float damage)
                {
                        SetDamageOverride(damage);
                }

                public override void Spawned()
                {
                        if (_abilityDefinition != null)
                        {
                                ConfigureImpactGraphic(_abilityDefinition.ImpactGraphic);
                        }

                        base.Spawned();
                }

                protected override void OnImpact(in LagCompensatedHit hit)
                {
                        base.OnImpact(hit);

                        string hitObjectName = hit.GameObject != null ? hit.GameObject.name : "<null>";
                        Debug.Log($"[IceShardProjectile] Impacted {hitObjectName}.");

                        if (Runner != null && Object != null && Object.IsValid == true)
                        {
                                Runner.Despawn(Object);
                        }
                }
        }
}
