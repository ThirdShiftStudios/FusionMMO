using UnityEngine;

namespace TPSBR
{
    [DisallowMultipleComponent]
    public class EnemyHealth : Health, IExperienceGiver
    {
        public bool IsAlive => base.IsAlive;

        public Vector3 ExperiencePosition => _hitIndicatorPivot != null ? _hitIndicatorPivot.position : transform.position;

        protected override void OnDeath(HitData hitData)
        {
            if (Context?.GameplayMode != null)
            {
                Context.GameplayMode.EnemyDeath(this, hitData);
            }
        }
    }
}
