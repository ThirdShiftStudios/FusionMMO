using UnityEngine;

namespace TPSBR
{
    [DisallowMultipleComponent]
    public class EnemyHealth : Health
    {
        public bool IsAlive => base.IsAlive;

        protected override void OnDeath(HitData hitData)
        {
            if (Context?.GameplayMode != null)
            {
                Context.GameplayMode.EnemyDeath(this, hitData);
            }
        }
    }
}
