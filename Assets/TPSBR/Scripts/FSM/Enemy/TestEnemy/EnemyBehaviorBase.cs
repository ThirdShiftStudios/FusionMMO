using Fusion.Addons.FSM;
using UnityEngine;

namespace TPSBR.Enemies
{
    public abstract class EnemyBehaviorBase : EnemyBehavior
    {
        [SerializeField]
        [Tooltip("Optional explicit next behavior to transition into when this state completes.")]
        private EnemyBehavior _defaultNext;

        protected EnemyBehavior DefaultNext => _defaultNext;

        protected EnemyBehavior ResolveDefaultNext<TBehavior>() where TBehavior : EnemyBehavior
        {
            if (_defaultNext != null)
                return _defaultNext;

            if (Controller == null)
                return null;

            for (int i = 0; i < Controller.Machine.States.Length; i++)
            {
                if (Controller.Machine.States[i] is TBehavior)
                {
                    return Controller.Machine.States[i];
                }
            }

            return null;
        }
    }
}