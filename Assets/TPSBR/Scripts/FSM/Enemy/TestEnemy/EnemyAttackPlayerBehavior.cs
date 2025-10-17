using Fusion;
using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyAttackPlayerBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Maximum distance from the target required to start the attack.")]
        private float _attackRange = 2f;

        [SerializeField]
        [Tooltip("Damage dealt per successful attack.")]
        private float _damagePerHit = 10f;

        [SerializeField]
        [Tooltip("Delay between consecutive attacks in seconds.")]
        private float _attackCooldown = 1f;

        [SerializeField]
        [Tooltip("Optional override for the layer mask used when looking for targets.")]
        private LayerMask _targetMask;

        [SerializeField]
        [Tooltip("Hit type reported when damaging the target.")]
        private EHitType _hitType = EHitType.Suicide;

        private readonly Collider[] _overlapResults = new Collider[8];
        private float _cooldownTimer;

        public float AttackRange => _attackRange;

        protected override void OnEnterState()
        {
            base.OnEnterState();

            _cooldownTimer = 0f;

            if (Controller is TestEnemy enemy)
            {
                enemy.StopNavigation();
            }
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HasStateAuthority == false)
                return;

            if (Controller is not TestEnemy enemy)
                return;

            if (enemy.HasPlayerTarget == false)
            {
                TransitionToChase(enemy);
                return;
            }

            if (IsWithinAttackRange(enemy) == false)
            {
                TransitionToChase(enemy);
                return;
            }

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Runner.DeltaTime);
                return;
            }

            if (PerformAttack(enemy) == true && _attackCooldown > 0f)
            {
                _cooldownTimer = _attackCooldown;
            }
        }

        public bool IsWithinAttackRange(TestEnemy enemy)
        {
            if (enemy == null || enemy.HasPlayerTarget == false)
                return false;

            Vector3 targetPosition = enemy.GetTargetPosition();
            Vector3 delta = targetPosition - enemy.transform.position;
            delta.y = 0f;

            float sqrAttackRange = _attackRange * _attackRange;
            return delta.sqrMagnitude <= sqrAttackRange;
        }

        private void TransitionToChase(TestEnemy enemy)
        {
            if (enemy == null)
                return;

            if (enemy.ChaseAgent != null)
            {
                Machine.ForceActivateState(enemy.ChaseAgent.StateId);
            }
            else if (enemy.Patrol != null)
            {
                Machine.ForceActivateState(enemy.Patrol.StateId);
            }
            else if (DefaultNext != null)
            {
                Machine.ForceActivateState(DefaultNext.StateId);
            }
        }

        private bool PerformAttack(TestEnemy enemy)
        {
            int mask = _targetMask;
            if (mask == 0)
            {
                mask = ObjectLayerMask.Agent;
            }

            var physicsScene = Runner.SimulationUnityScene.GetPhysicsScene();
            int hitCount = physicsScene.OverlapSphere(enemy.transform.position, _attackRange, _overlapResults, mask, QueryTriggerInteraction.UseGlobal);

            bool hitAny = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _overlapResults[i];
                if (collider == null)
                    continue;

                if (collider.transform.IsChildOf(enemy.transform) == true)
                    continue;

                Agent agent = collider.GetComponentInParent<Agent>();
                if (agent == null)
                    continue;

                if (HitUtility.ProcessHit(enemy, collider, _damagePerHit, _hitType, out HitData _))
                {
                    hitAny = true;
                }
            }

            return hitAny;
        }
    }
}
