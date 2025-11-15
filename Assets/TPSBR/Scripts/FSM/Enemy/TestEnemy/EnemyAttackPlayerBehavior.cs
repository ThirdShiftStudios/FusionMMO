using Fusion;
using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyAttackPlayerBehavior : SingleClipBehavior
    {
        [SerializeField]
        [Tooltip("Transform used as the origin of the attack sphere.")]
        private Transform _attackOrigin;

        [SerializeField]
        [Tooltip("Radius of the sphere used to detect targets when attacking.")]
        private float _attackRadius = 2f;

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

        private float _cooldownTimer;
        private bool _shouldChase;

        public float AttackRange => _attackRadius;
        public bool ShouldChase => _shouldChase;
        bool _attackTriggered = false;

        protected override void OnEnterStateRender()
        {
            base.OnEnterStateRender();
            _attackTriggered = false;
        }
        protected override void OnRender()
        {
            base.OnRender();
            if(_attackTriggered == false && AnimationNormalizedTime >= 0.5f)
            {
                Debug.Log($"Render: Attack Trigger : {Runner.Tick}");
                _attackTriggered = true;
            }
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

            if (_attackTriggered == false && AnimationNormalizedTime >= 0.5f)
            {
                Debug.Log($"FixedUpdate: Attack Trigger : {Runner.Tick}");
                _attackTriggered = true;
            }

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

            Vector3 originPosition = GetAttackOriginPosition(enemy);
            Vector3 targetPosition = enemy.GetTargetPosition();
            Vector3 delta = targetPosition - originPosition;
            delta.y = 0f;

            float sqrAttackRange = _attackRadius * _attackRadius;
            return delta.sqrMagnitude <= sqrAttackRange;
        }

        private bool PerformAttack(TestEnemy enemy)
        {
            int mask = _targetMask;
            if (mask == 0)
            {
                mask = ObjectLayerMask.Agent;
            }

            var hits = ListPool.Get<LagCompensatedHit>(8);
            var hitRoots = ListPool.Get<int>(8);

            Vector3 originPosition = GetAttackOriginPosition(enemy);
            int hitCount = Runner.LagCompensation.OverlapSphere(originPosition, _attackRadius, enemy.Object.InputAuthority, hits, mask);

            bool hitAny = false;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hits[i];

                if (hit.Hitbox == null)
                    continue;

                var hitRoot = hit.Hitbox.Root;
                if (hitRoot == null)
                    continue;

                if (hitRoot.transform.IsChildOf(enemy.transform) == true)
                    continue;

                int hitRootID = hitRoot.GetInstanceID();
                if (hitRoots.Contains(hitRootID) == true)
                    continue;

                Vector3 direction = hit.Point - originPosition;
                float magnitude = direction.magnitude;
                if (magnitude > 0.001f)
                {
                    direction /= magnitude;
                }
                else
                {
                    direction = enemy.transform.forward;
                }

                if (HitUtility.ProcessHit(enemy.Object, direction, hit, _damagePerHit, _hitType, out HitData _))
                {
                    hitAny = true;
                    hitRoots.Add(hitRootID);
                }
            }

            ListPool.Return(hitRoots);
            ListPool.Return(hits);

            return hitAny;
        }

        private Vector3 GetAttackOriginPosition(TestEnemy enemy)
        {
            if (_attackOrigin != null)
            {
                return _attackOrigin.position;
            }

            return enemy.transform.position;
        }
    }
}
