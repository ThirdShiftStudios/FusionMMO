using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class FireStormAbility : ContextBehaviour
    {
        [SerializeField]
        private float _defaultRadius = 5f;
        [SerializeField]
        private float _defaultDamage = 15f;
        [SerializeField]
        private float _defaultDuration = 5f;
        [SerializeField]
        private float _defaultTickInterval = 0.75f;

        [Networked]
        private TickTimer _lifeTimer { get; set; }

        [Networked]
        private TickTimer _tickTimer { get; set; }

        private NetworkObject _owner;
        private LayerMask _hitMask;
        private EHitType _hitType;
        private float _radius;
        private float _damage;
        private float _tickInterval;
        private GameObject _impactGraphic;
        private BuffDefinition _buffDefinition;
        private readonly HashSet<IHitTarget> _processedTargets = new HashSet<IHitTarget>();

        public void Configure(NetworkObject owner, LayerMask hitMask, EHitType hitType, float damage, float duration, float radius, float tickInterval, GameObject impactGraphic)
        {
            _owner = owner;
            _hitMask = hitMask;
            _hitType = hitType;

            _impactGraphic = impactGraphic;
            _damage = damage > 0f ? damage : _defaultDamage;
            _radius = radius > 0f ? radius : _defaultRadius;
            _tickInterval = tickInterval > 0f ? tickInterval : _defaultTickInterval;

            float resolvedDuration = duration > 0f ? duration : _defaultDuration;

            if (Runner != null && Runner.IsRunning == true)
            {
                _lifeTimer = TickTimer.CreateFromSeconds(Runner, resolvedDuration);
                ResetTickTimer();
            }
        }

        public void ConfigureBuff(BuffDefinition buffDefinition)
        {
            _buffDefinition = buffDefinition;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (HasStateAuthority == false)
            {
                return;
            }

            if (_lifeTimer.ExpiredOrNotRunning(Runner) == true)
            {
                if (Object != null && Object.IsValid == true)
                {
                    Runner.Despawn(Object);
                }

                return;
            }

            if (_tickTimer.ExpiredOrNotRunning(Runner) == true)
            {
                ApplyDamage();
                ResetTickTimer();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            _owner = null;
            _hitMask = default;
            _hitType = default;
            _radius = _defaultRadius;
            _damage = _defaultDamage;
            _tickInterval = _defaultTickInterval;
            _impactGraphic = null;
            _buffDefinition = null;
            _lifeTimer = default;
            _tickTimer = default;
            _processedTargets.Clear();
        }

        private void ApplyDamage()
        {
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
            NetworkObject fireStormObject = Object;
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

                ApplyBuff(target);
                SpawnImpact(target);

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
                else if (fireStormObject != null)
                {
                    HitUtility.ProcessHit(fireStormObject.InputAuthority, direction, hit, _damage, hitType, out HitData _);
                }
            }

            ListPool.Return(hits);
        }

        private void ResetTickTimer()
        {
            if (Runner != null && Runner.IsRunning == true)
            {
                _tickTimer = TickTimer.CreateFromSeconds(Runner, _tickInterval);
            }
        }

        private void SpawnImpact(IHitTarget target)
        {
            if (_impactGraphic == null || target == null || Context == null || Context.ObjectCache == null)
            {
                return;
            }

            Transform parent = target.AbilityHitPivot != null ? target.AbilityHitPivot : target.HitPivot;
            if (parent == null)
            {
                return;
            }

            var impactInstance = Context.ObjectCache.Get(_impactGraphic);
            impactInstance.transform.SetParent(parent, false);
            impactInstance.transform.localPosition = Vector3.zero;
            impactInstance.transform.localRotation = Quaternion.identity;
            Context.ObjectCache.ReturnDeferred(impactInstance, 5f);
        }

        private void ApplyBuff(IHitTarget target)
        {
            if (_buffDefinition == null || target == null)
            {
                return;
            }

            if (target is Component component)
            {
                var buffSystem = component.GetComponent<BuffSystem>();
                buffSystem?.ApplyBuff(_buffDefinition);
            }
        }
    }
}
