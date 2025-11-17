using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class FireNova : ContextBehaviour
    {

        [SerializeField] private Transform _visualsRoot;
        [SerializeField] private Transform _scalarRoot;
        [SerializeField] private float _radius = 4f;
        [SerializeField] private float _damage = 75f;
        [SerializeField] private float _duration = 1.5f;
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Networked]
        private TickTimer _lifeTimer { get; set; }

        [Networked(OnChanged = nameof(OnNetworkStatsChanged))]
        private FireNovaStats _networkStats { get; set; }

        private readonly HashSet<IHitTarget> _processedTargets = new HashSet<IHitTarget>();

        private Vector3 _initialScalarScale = Vector3.one;
        private bool _damageApplied;
        private NetworkObject _owner;
        private LayerMask _hitMask;
        private EHitType _hitType;
        private float _visualTimer;
        private float _burnDuration;
        private float _burnDamage;

        public override void Spawned()
        {
            base.Spawned();

            CacheInitialReferences();
            ResetVisualState();
            ApplyStatsFromNetwork();
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            AdvanceVisualTimer(true);

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

            AdvanceVisualTimer(false);
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
                UpdateInitialScaleFromRadius();
            }
        }

        private void ResetVisualState()
        {
            _visualTimer = 0f;

            if (_scalarRoot != null)
            {
                _scalarRoot.localScale = Vector3.zero;
            }

            UpdateVisualScale();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _processedTargets.Clear();
            _damageApplied = false;
            _owner = null;
            _hitMask = default;
            _hitType = default;
            _lifeTimer = default;
            _visualTimer = 0f;
            _burnDuration = 0f;
            _burnDamage = 0f;

            if (_scalarRoot != null)
            {
                _scalarRoot.localScale = _initialScalarScale;
            }

            base.Despawned(runner, hasState);
        }

        public void ConfigureDamage(float damage)
        {
            if (damage > 0f)
            {
                _damage = damage;
            }

            UpdateNetworkStats(damageValue: Mathf.Max(0f, damage));
        }

        public void ConfigureLevel(float radius, float burnDuration, float burnDamage)
        {
            if (radius > 0f)
            {
                _radius = radius;
                UpdateInitialScaleFromRadius();
            }

            _burnDuration = Mathf.Max(0f, burnDuration);
            _burnDamage = Mathf.Max(0f, burnDamage);

            UpdateNetworkStats(radiusValue: Mathf.Max(0f, radius), burnDurationValue: _burnDuration, burnDamageValue: _burnDamage);
        }

        public void StartNova(NetworkObject owner, Vector3 firePosition, LayerMask hitMask, EHitType staffWeaponHitType)
        {
            _owner = owner;
            _hitMask = hitMask;
            _hitType = staffWeaponHitType;
            _damageApplied = false;

            transform.SetPositionAndRotation(firePosition, Quaternion.identity);

            ResetVisualState();

            if (Runner != null && Runner.IsRunning == true)
            {
                float lifeDuration = Mathf.Max(0.01f, _duration);
                _lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeDuration);
                Debug.Log("Starting Nova");
            }
            else
            {
                _lifeTimer = default;
            }

            AdvanceVisualTimer(true);
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

            if (Runner == null || Runner.IsRunning == false)
            {
                return;
            }

            HitboxManager lagCompensation = Runner.LagCompensation;

            if (lagCompensation == null)
            {
                return;
            }

            int mask = _hitMask.value;
            if (mask == 0)
            {
                mask = Physics.AllLayers;
            }

            Vector3 position = transform.position;

            PlayerRef authority = _owner != null ? _owner.InputAuthority : (Object != null ? Object.InputAuthority : PlayerRef.None);
            var hits = ListPool.Get<LagCompensatedHit>(32);
            int hitCount = lagCompensation.OverlapSphere(position, _radius, authority, hits, mask);

            if (hitCount <= 0)
            {
                ListPool.Return(hits);
                return;
            }

            _processedTargets.Clear();

            NetworkObject ownerObject = _owner;
            NetworkObject fireNovaObject = Object;
            EHitType hitType = _hitType != EHitType.None ? _hitType : EHitType.Grenade;

            for (int i = 0; i < hitCount; i++)
            {
                LagCompensatedHit hit = hits[i];

                if (hit.Hitbox == null)
                {
                    continue;
                }

                NetworkObject hitRoot = hit.Hitbox.Root.Object;

                if (hitRoot == null)
                {
                    continue;
                }

                if (ownerObject != null && hitRoot == ownerObject)
                {
                    continue;
                }

                IHitTarget target = hitRoot.GetComponent<IHitTarget>();

                if (target == null)
                {
                    continue;
                }

                if (_processedTargets.Add(target) == false)
                {
                    continue;
                }

                Vector3 point = hit.Point;
                if (point == default)
                {
                    point = hitRoot.transform.position;
                }

                Vector3 direction = point - position;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector3.up;
                }
                else
                {
                    direction.Normalize();
                }

                if (ownerObject != null)
                {
                    HitUtility.ProcessHit(ownerObject, direction, hit, _damage, hitType, out HitData _);
                }
                else if (fireNovaObject != null)
                {
                    HitUtility.ProcessHit(fireNovaObject.InputAuthority, direction, hit, _damage, hitType, out HitData _);
                }
            }

            ListPool.Return(hits);
        }

        private void AdvanceVisualTimer(bool clampToNetworkTime)
        {
            if (_duration <= 0f)
            {
                _visualTimer = _duration;
                return;
            }

            if (Runner != null && Runner.IsRunning == true && _lifeTimer.IsRunning == true)
            {
                float remaining = _lifeTimer.RemainingTime(Runner) ?? 0f;
                _visualTimer = Mathf.Clamp(_duration - remaining, 0f, _duration);
                return;
            }

            if (clampToNetworkTime == true)
            {
                return;
            }

            float deltaTime = Runner != null && Runner.IsRunning == true ? Runner.DeltaTime : Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            _visualTimer = Mathf.Min(_visualTimer + deltaTime, _duration);
        }

        private void UpdateVisualScale()
        {
            if (_scalarRoot == null)
            {
                return;
            }

            float normalizedLifetime = _duration > 0f ? Mathf.Clamp01(_visualTimer / _duration) : 1f;
            float scaleMultiplier = _scaleCurve != null && _scaleCurve.length > 0 ? _scaleCurve.Evaluate(normalizedLifetime) : normalizedLifetime;
            scaleMultiplier = Mathf.Max(0f, scaleMultiplier);

            Vector3 baseScale = _initialScalarScale;
            _scalarRoot.localScale = new Vector3(baseScale.x * scaleMultiplier, baseScale.y, baseScale.z * scaleMultiplier);
        }

        private void UpdateInitialScaleFromRadius()
        {
            float diameter = Mathf.Max(0f, _radius) * 2f;
            _initialScalarScale = new Vector3(diameter, 1f, diameter);
        }

        private void ApplyStatsFromNetwork()
        {
            var stats = _networkStats;

            if (stats.Radius > 0f)
            {
                _radius = stats.Radius;
                UpdateInitialScaleFromRadius();
            }

            if (stats.Damage > 0f)
            {
                _damage = stats.Damage;
            }

            _burnDuration = Mathf.Max(0f, stats.BurnDuration);
            _burnDamage = Mathf.Max(0f, stats.BurnDamage);

            UpdateVisualScale();
        }

        private void UpdateNetworkStats(float radiusValue = -1f, float damageValue = -1f, float burnDurationValue = -1f, float burnDamageValue = -1f)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            var stats = _networkStats;

            if (radiusValue >= 0f)
            {
                stats.Radius = radiusValue;
            }

            if (damageValue >= 0f)
            {
                stats.Damage = damageValue;
            }

            if (burnDurationValue >= 0f)
            {
                stats.BurnDuration = burnDurationValue;
            }

            if (burnDamageValue >= 0f)
            {
                stats.BurnDamage = burnDamageValue;
            }

            _networkStats = stats;
        }

        private static void OnNetworkStatsChanged(Changed<FireNova> changed)
        {
            changed.Behaviour.ApplyStatsFromNetwork();
        }

        private struct FireNovaStats : INetworkStruct
        {
            public float Radius;
            public float Damage;
            public float BurnDuration;
            public float BurnDamage;
        }


    }
}
