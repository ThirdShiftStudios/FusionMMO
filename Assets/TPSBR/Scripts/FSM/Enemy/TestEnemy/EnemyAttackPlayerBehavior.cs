using Fusion;
using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyAttackPlayerBehavior : SingleClipBehavior
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
        [Range(0f, 1f)]
        [Tooltip("Normalized time inside the attack animation when the hit should occur.")]
        private float _attackTime = 0.5f;

        [SerializeField]
        [Tooltip("Optional override for the layer mask used when looking for targets.")]
        private LayerMask _targetMask;

        [SerializeField]
        [Tooltip("Hit type reported when damaging the target.")]
        private EHitType _hitType = EHitType.Suicide;

        private readonly Collider[] _overlapResults = new Collider[8];
        private float _cooldownTimer;
        private bool _shouldChase;

        public float AttackRange => _attackRange;
        public bool ShouldChase => _shouldChase;
        bool _attackTriggered = false;

        protected override void OnEnterStateRender()
        {
            base.OnEnterStateRender();
            _attackTriggered = false;
        }
        protected override void OnEnterState()
        {
            base.OnEnterState();
            _attackTriggered = false;
            _cooldownTimer = 0f;
            _shouldChase = false;

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

            _shouldChase = false;

            if (enemy.HasPlayerTarget == false)
            {
                _shouldChase = true;
                return;
            }

            if (IsWithinAttackRange(enemy) == false)
            {
                _shouldChase = true;
                return;
            }

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Runner.DeltaTime);
                return;
            }

            if (AnimationNormalizedTime < _attackTime)
            {
                return;
            }

            if (_attackTriggered == true)
            {
                return;
            }

            Debug.Log($"FixedUpdate: Attack Trigger : {Runner.Tick}");
            _attackTriggered = true;

            if (PerformAttack(enemy) == true && _attackCooldown > 0f)
            {
                _cooldownTimer = _attackCooldown;
            }
        }

        protected override void OnExitState()
        {
            base.OnExitState();

            _shouldChase = false;
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
