using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class FireNova : ContextBehaviour
    {
        private static readonly Collider[] _colliderCache = new Collider[32];

        [SerializeField] private Transform _visualsRoot;
        [SerializeField] private Transform _scalarRoot;
        [SerializeField] private float _radius = 4f;
        [SerializeField] private float _damage = 75f;
        [SerializeField] private float _duration = 1.5f;
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Networked]
        private TickTimer _lifeTimer { get; set; }

        private readonly HashSet<IHitTarget> _processedTargets = new HashSet<IHitTarget>();

        private Vector3 _initialScalarScale = Vector3.one;
        private bool _damageApplied;
        private NetworkObject _owner;
        private LayerMask _hitMask;
        private EHitType _hitType;

        public override void Spawned()
        {
            base.Spawned();

            CacheInitialReferences();
            if (_scalarRoot != null)
            {
                _scalarRoot.localScale = Vector3.zero;
            }
            UpdateVisualScale();
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            UpdateVisualScale();

            if (HasStateAuthority == true)
            {
                if (_damageApplied == false)
                {
                    ApplyDamage();
                }

                if (_lifeTimer.ExpiredOrNotRunning(Runner) == true && Object != null && Object.IsValid == true)
                {
                    Runner.Despawn(Object);
                }
            }
        }

        public override void Render()
        {
            base.Render();

            UpdateVisualScale();
        }

        private void CacheInitialReferences()
        {
            if (_visualsRoot == null)
            {
                _visualsRoot = transform;
            }

            if (_scalarRoot == null)
            {
                _scalarRoot = _visualsRoot;
            }

            if (_scalarRoot != null)
            {
                _initialScalarScale = _scalarRoot.localScale;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _processedTargets.Clear();
            _damageApplied = false;
            _owner = null;
            _hitMask = default;
            _hitType = default;
            _lifeTimer = default;
            base.Despawned(runner, hasState);
        }

        public void StartNova(NetworkObject owner, Vector3 firePosition, LayerMask hitMask, EHitType staffWeaponHitType)
        {
            _owner = owner;
            _hitMask = hitMask;
            _hitType = staffWeaponHitType;
            _damageApplied = false;

            transform.SetPositionAndRotation(firePosition, Quaternion.identity);

            if (_scalarRoot != null)
            {
                _scalarRoot.localScale = Vector3.zero;
            }

            if (Runner != null && Runner.IsRunning == true)
            {
                float lifeDuration = Mathf.Max(0.01f, _duration);
                _lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeDuration);
            }
            else
            {
                _lifeTimer = default;
            }

            UpdateVisualScale();

            if (HasStateAuthority == true)
            {
                ApplyDamage();
            }
        }

        private void ApplyDamage()
        {
            if (_damageApplied == true)
            {
                return;
            }

            _damageApplied = true;

            if (Runner == null)
            {
                return;
            }

            PhysicsScene physicsScene = Runner.SimulationUnityScene.GetPhysicsScene();

            if (physicsScene.IsValid() == false)
            {
                return;
            }

            int mask = _hitMask.value;
            if (mask == 0)
            {
                mask = Physics.AllLayers;
            }

            Vector3 position = transform.position;
            int hitCount = physicsScene.OverlapSphere(position, _radius, _colliderCache, mask, QueryTriggerInteraction.UseGlobal);

            if (hitCount <= 0)
            {
                return;
            }

            PlayerRef instigatorRef = _owner != null ? _owner.InputAuthority : (Object != null ? Object.InputAuthority : PlayerRef.None);
            IHitInstigator instigator = _owner != null ? _owner.GetComponent<IHitInstigator>() : GetComponent<IHitInstigator>();
            EHitType hitType = _hitType != EHitType.None ? _hitType : EHitType.Grenade;

            _processedTargets.Clear();

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _colliderCache[i];

                if (collider == null)
                {
                    continue;
                }

                if (_owner != null && collider.transform.IsChildOf(_owner.transform) == true)
                {
                    continue;
                }

                IHitTarget target = collider.GetComponentInParent<IHitTarget>();

                if (target == null)
                {
                    continue;
                }

                if (_processedTargets.Add(target) == false)
                {
                    continue;
                }

                Vector3 targetPosition = collider.transform.position;
                Vector3 direction = (targetPosition - position).normalized;

                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector3.up;
                }

                HitData hitData = new HitData
                {
                    Action = EHitAction.Damage,
                    Amount = _damage,
                    Position = targetPosition,
                    Normal = -direction,
                    Direction = direction,
                    Target = target,
                    InstigatorRef = instigatorRef,
                    Instigator = instigator,
                    HitType = hitType,
                };

                HitUtility.ProcessHit(ref hitData);
            }
        }

        private void UpdateVisualScale()
        {
            if (_scalarRoot == null)
            {
                return;
            }

            float normalizedLifetime = GetNormalizedLifetime();
            float scaleMultiplier = _scaleCurve != null && _scaleCurve.length > 0 ? _scaleCurve.Evaluate(normalizedLifetime) : normalizedLifetime;
            scaleMultiplier = Mathf.Max(0f, scaleMultiplier);

            Vector3 baseScale = _initialScalarScale;
            _scalarRoot.localScale = new Vector3(baseScale.x * scaleMultiplier, baseScale.y, baseScale.z * scaleMultiplier);
        }

        private float GetNormalizedLifetime()
        {
            if (_duration <= 0f || Runner == null)
            {
                return 1f;
            }

            if (_lifeTimer.IsRunning == false)
            {
                return 0f;
            }

            float remaining = _lifeTimer.RemainingTime(Runner) ?? 0f;
            float elapsed = Mathf.Max(0f, _duration - remaining);
            return Mathf.Clamp01(elapsed / _duration);
        }
    }
}
