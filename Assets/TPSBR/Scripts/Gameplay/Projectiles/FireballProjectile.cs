using Fusion;
using UnityEngine;

namespace TPSBR
{
        public class FireballProjectile : KinematicProjectile
        {
                protected override void OnImpact(in LagCompensatedHit hit)
                {
                        base.OnImpact(hit);

                        string hitObjectName = hit.GameObject != null ? hit.GameObject.name : "<null>";
                        Debug.Log($"[FireballProjectile] Impacted {hitObjectName}.");

                        if (Runner != null && Object != null && Object.IsValid == true)
                        {
                                Runner.Despawn(Object);
                        }
                }
        }
}
