using Fusion;
using UnityEngine;

namespace TPSBR
{
        public class IceShardProjectile : KinematicProjectile
        {
                public void ConfigureDamage(float damage)
                {
                        SetDamageOverride(damage);
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
