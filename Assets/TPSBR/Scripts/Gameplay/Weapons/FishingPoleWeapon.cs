using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class FishingPoleWeapon : Weapon
    {
        [SerializeField]
        private float _holdToCastDuration = 1f;
        [SerializeField]
        private Transform _lureFireTransform;
        [SerializeField]
        private NetworkPrefabRef _lureProjectilePrefab;
        [SerializeField]
        private float _lureProjectileSpeed = 12f;
        [SerializeField]
        private ParabolaString _parabolaString;
        [SerializeField]
        private Renderer[] _renderers;
        [SerializeField]
        private float _waitingSuccessZoneBobAmplitude = 0.1f;
        [SerializeField]
        private float _waitingSuccessZoneBobFrequency = 2f;

        [Networked]
        private FishingLureProjectile NetworkedActiveLure { get; set; }

        private float _primaryHoldTime;
        private bool _isPrimaryHeld;
        private bool _castRequested;
        private bool _castActive;
        private bool _waitingForPrimaryRelease;
        private FishingLureProjectile _activeLureProjectile;
        private bool _lureLaunched;
        private bool _renderersResolved;
        private bool _isInWaitingPhase;
        private bool _isHookSetSuccessZoneActive;
        private float _waitingBobTimer;
        private bool _waitingBobOffsetApplied;

        [Networked]
        private NetworkBool NetworkedHookSetSuccessZoneActive { get; set; }

        public event Action<FishingLifecycleState> LifecycleStateChanged;

        public override void Spawned()
        {
            base.Spawned();
            HandleActiveLureChanged(true);
        }

        public override void Render()
        {
            base.Render();
            HandleActiveLureChanged();
            ApplyHookSetSuccessZoneState(NetworkedHookSetSuccessZoneActive);
            UpdateWaitingLureBob();
        }

        private void OnEnable()
        {
            if (_parabolaString == null)
            {
                _parabolaString = GetComponent<ParabolaString>();
            }

            _parabolaString?.ClearEndpoints();

            ResolveRenderers();
            SetRenderersVisible(IsArmed);
        }

        private void OnDisable()
        {
            _parabolaString?.ClearEndpoints();
            SetRenderersVisible(false);
        }

        private void Awake()
        {
            ResolveRenderers();
            SetRenderersVisible(false);
            ResetWaitingBobVisuals();
        }

        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
            // Fishing pole currently has no firing behaviour. Override when casting logic is implemented.
        }

        protected override void OnWeaponArmed()
        {
            base.OnWeaponArmed();
            SetRenderersVisible(true);
        }

        protected override void OnWeaponDisarmed()
        {
            base.OnWeaponDisarmed();
            SetRenderersVisible(false);
        }

        public override WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            if (Character == null || Character.Agent == null)
            {
                ResetHoldTracking();
                _castRequested = false;
                _castActive = false;
                _waitingForPrimaryRelease = false;
                return WeaponUseRequest.None;
            }

            UseLayer layer = null;
            bool waitingInterrupted = false;

            if (attackActivated == true)
            {
                _waitingForPrimaryRelease = false;
            }

            if (attackActivated == true && _castActive == false)
            {
                layer = GetUseLayer();

                if (layer?.FishingPoleUseState != null)
                {
                    waitingInterrupted = layer.FishingPoleUseState.TryInterruptWaitingForNewCast(this);

                    if (waitingInterrupted == true)
                    {
                        _waitingForPrimaryRelease = false;
                    }
                }
            }

            if (attackReleased == true)
            {
                _isPrimaryHeld = false;
                _primaryHoldTime = 0f;
                _waitingForPrimaryRelease = false;
            }
            else if (attackHeld == true)
            {
                if (_castActive == false && _castRequested == false && waitingInterrupted == false)
                {
                    layer ??= GetUseLayer();

                    if (layer?.FishingPoleUseState != null && layer.FishingPoleUseState.TryInterruptWaitingForNewCast(this) == true)
                    {
                        waitingInterrupted = true;
                        _waitingForPrimaryRelease = false;
                    }
                }

                if (_waitingForPrimaryRelease == true)
                {
                    return WeaponUseRequest.None;
                }

                if (_isPrimaryHeld == false)
                {
                    _isPrimaryHeld = true;
                    _primaryHoldTime = 0f;
                }

                if (_castActive == false && _castRequested == false)
                {
                    _primaryHoldTime += GetDeltaTime();

                    if (_primaryHoldTime >= _holdToCastDuration)
                    {
                        _castRequested = true;
                        return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.FishingCast);
                    }
                }
            }
            else
            {
                ResetHoldTracking();
            }

            return WeaponUseRequest.None;
        }

        public override bool HandleAnimationRequest(UseLayer attackLayer, in WeaponUseRequest request)
        {
            if (request.Animation == WeaponUseAnimation.FishingCast)
            {
                FishingPoleUseState fishingUse = attackLayer?.FishingPoleUseState;

                if (fishingUse == null)
                {
                    _castRequested = false;
                    return false;
                }

                if (fishingUse.BeginCast(this) == false)
                {
                    _castRequested = false;
                    return false;
                }

                return true;
            }

            return base.HandleAnimationRequest(attackLayer, request);
        }

        internal void NotifyCastStarted()
        {
            _castRequested = false;
            _castActive = true;
            _lureLaunched = false;
            _waitingForPrimaryRelease = false;
            EndWaitingPhase();
            CleanupLure(false);

            RaiseLifecycleStateChanged(FishingLifecycleState.Casting);
        }

        internal void NotifyCastThrown()
        {
            // The cast remains active until the animation finishes.
            RaiseLifecycleStateChanged(FishingLifecycleState.LureInFlight);
        }

        internal void NotifyCastCompleted()
        {
            _castActive = false;
            _castRequested = false;
            _waitingForPrimaryRelease = false;
            ResetHoldTracking();
            EndWaitingPhase();
            CleanupLure(true);

            RaiseLifecycleStateChanged(FishingLifecycleState.Ready);
        }

        internal void NotifyCastCancelled()
        {
            _castActive = false;
            _castRequested = false;
            ResetHoldTracking();
            _waitingForPrimaryRelease = true;
            EndWaitingPhase();
            CleanupLure(true);

            RaiseLifecycleStateChanged(FishingLifecycleState.Ready);
        }

        internal void NotifyWaitingPhaseEntered()
        {
            _castActive = false;
            _waitingForPrimaryRelease = false;
            ResetHoldTracking();
            _isInWaitingPhase = true;
            SetHookSetSuccessZoneState(false);
            ResetWaitingBobVisuals();

            RaiseLifecycleStateChanged(FishingLifecycleState.Waiting);
        }

        internal void EnterFightingPhase()
        {
            UseLayer layer = GetUseLayer();

            if (layer?.FishingPoleUseState == null)
                return;

            EndWaitingPhase();
            layer.FishingPoleUseState.EnterFightingPhase(this);
        }

        internal void EnterCatchPhase()
        {
            UseLayer layer = GetUseLayer();

            if (layer?.FishingPoleUseState == null)
                return;

            EndWaitingPhase();
            layer.FishingPoleUseState.EnterCatchPhase(this);
        }

        internal void HandleHookSetFailed()
        {
            UseLayer layer = GetUseLayer();

            if (layer?.FishingPoleUseState != null && layer.FishingPoleUseState.TryCancelActiveCast(this) == true)
            {
                return;
            }

            EndWaitingPhase();
            RaiseLifecycleStateChanged(FishingLifecycleState.Ready);
        }

        internal void HandleFightingFailed()
        {
            UseLayer layer = GetUseLayer();

            if (layer?.FishingPoleUseState != null && layer.FishingPoleUseState.TryCancelActiveCast(this) == true)
            {
                return;
            }

            EndWaitingPhase();
            RaiseLifecycleStateChanged(FishingLifecycleState.Ready);
        }

        internal void NotifyFightingPhaseEntered()
        {
            EndWaitingPhase();
            RaiseLifecycleStateChanged(FishingLifecycleState.Fighting);
        }

        internal void LaunchLure()
        {
            if (_castActive == false || _lureLaunched == true)
                return;

            if (HasStateAuthority == false)
                return;

            if (_lureFireTransform == null)
                return;

            if (_lureProjectilePrefab.IsValid == false)
                return;

            NetworkRunner runner = Runner;

            if (runner == null || runner.IsRunning == false)
                return;

            NetworkObject owner = Owner;

            if (owner == null)
                return;

            Transform fireTransform = Character.ThirdPersonView.FireTransform;
            Transform cameraTransform = Character.ThirdPersonView.DefaultCameraTransform;

            if (fireTransform == null || cameraTransform == null)
            {
                return;
            }

            Vector3 firePosition = fireTransform.position;
            Vector3 targetPoint = cameraTransform.position + cameraTransform.forward * 100f;
            Vector3 direction = targetPoint - firePosition;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Character != null ? Character.transform.forward : Vector3.forward;
            }

            direction.Normalize();

            Vector3 initialVelocity = direction * _lureProjectileSpeed;
            LayerMask hitMask = Character != null && Character.Agent != null && Character.Agent.Inventory != null ? Character.Agent.Inventory.HitMask : default;

            // Ensure the lure can collide with water volumes regardless of the inventory configuration.
            hitMask |= 1 << ObjectLayer.Water;

            _lureLaunched = true;

            runner.Spawn(_lureProjectilePrefab, firePosition, Quaternion.LookRotation(direction), owner.InputAuthority, (spawnRunner, spawnedObject) =>
            {
                FishingLureProjectile projectile = spawnedObject.GetComponent<FishingLureProjectile>();

                if (projectile == null)
                {
                    _lureLaunched = false;
                    UpdateParabolaString();
                    return;
                }

                _activeLureProjectile = projectile;
                projectile.Initialize(this);
                projectile.Fire(owner, firePosition, initialVelocity, hitMask, HitType);
                NetworkedActiveLure = projectile;
                UpdateParabolaString();
            });
        }

        internal void OnLureImpacted(FishingLureProjectile projectile, in LagCompensatedHit hit)
        {
            if (_castActive == false)
                return;

            bool hitWater = hit.GameObject != null && hit.GameObject.layer == ObjectLayer.Water;

            UseLayer layer = GetUseLayer();

            if (hitWater == true)
            {
                if (layer?.FishingPoleUseState != null)
                {
                    layer.FishingPoleUseState.EnterWaitingPhase(this);
                }

                _lureLaunched = false;
                UpdateParabolaString();
            }
            else
            {
                bool canceled = layer?.FishingPoleUseState != null &&
                                 layer.FishingPoleUseState.TryCancelActiveCast(this) == true;

                if (canceled == false && projectile != null && projectile == _activeLureProjectile)
                {
                    _activeLureProjectile = null;
                }

                if (canceled == false)
                {
                    CleanupLure(false);
                }
            }
        }

        private void ResetHoldTracking()
        {
            _isPrimaryHeld = false;
            _primaryHoldTime = 0f;
        }

        private float GetDeltaTime()
        {
            if (Runner != null)
            {
                return Runner.DeltaTime;
            }

            return Time.deltaTime;
        }

        private void CleanupLure(bool forceDespawn)
        {
            ResetWaitingBobVisuals();

            if (_activeLureProjectile != null)
            {
                if (forceDespawn == true && Runner != null && Runner.IsRunning == true)
                {
                    NetworkObject projectileObject = _activeLureProjectile.Object;

                    if (projectileObject != null && projectileObject.IsValid == true)
                    {
                        Runner.Despawn(projectileObject);
                    }
                }

                _activeLureProjectile = null;
            }

            _lureLaunched = false;
            if (HasStateAuthority == true)
            {
                NetworkedActiveLure = null;
            }
            UpdateParabolaString();
        }

        private void RaiseLifecycleStateChanged(FishingLifecycleState state)
        {
            LifecycleStateChanged?.Invoke(state);

            if (HasStateAuthority == false)
                return;

            NetworkObject networkObject = Object;

            if (networkObject == null || networkObject.HasInputAuthority == true)
                return;

            PlayerRef inputAuthority = networkObject.InputAuthority;

            if (inputAuthority == PlayerRef.None)
                return;

            RPC_ReportLifecycleStateChanged(state);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RPC_ReportLifecycleStateChanged(FishingLifecycleState state)
        {
            LifecycleStateChanged?.Invoke(state);
        }

        public void SetHookSetSuccessZoneState(bool isInSuccessZone)
        {
            bool shouldActivate = isInSuccessZone && _isInWaitingPhase;

            if (HasStateAuthority == true)
            {
                if (NetworkedHookSetSuccessZoneActive != shouldActivate)
                {
                    NetworkedHookSetSuccessZoneActive = shouldActivate;
                }
            }

            ApplyHookSetSuccessZoneState(shouldActivate);
        }

        private void ApplyHookSetSuccessZoneState(bool shouldActivate)
        {
            if (_isHookSetSuccessZoneActive == shouldActivate)
            {
                return;
            }

            _isHookSetSuccessZoneActive = shouldActivate;

            if (_isHookSetSuccessZoneActive == false)
            {
                ResetWaitingBobVisuals();
            }
        }

        private void EndWaitingPhase()
        {
            if (_isInWaitingPhase == false && _isHookSetSuccessZoneActive == false)
            {
                ResetWaitingBobVisuals();
                return;
            }

            _isInWaitingPhase = false;
            SetHookSetSuccessZoneState(false);
            ResetWaitingBobVisuals();
        }

        private void ResetWaitingBobVisuals()
        {
            _waitingBobTimer = 0f;
            _waitingBobOffsetApplied = false;

            if (_activeLureProjectile != null)
            {
                _activeLureProjectile.SetVisualOffset(Vector3.zero);
            }
        }

        private void UpdateWaitingLureBob()
        {
            FishingLureProjectile lure = _activeLureProjectile;

            if (lure == null)
            {
                return;
            }

            if (_isHookSetSuccessZoneActive == false)
            {
                if (_waitingBobOffsetApplied == true)
                {
                    lure.SetVisualOffset(Vector3.zero);
                    _waitingBobOffsetApplied = false;
                }

                _waitingBobTimer = 0f;
                return;
            }

            float frequency = Mathf.Max(0f, _waitingSuccessZoneBobFrequency);
            _waitingBobTimer += Time.deltaTime;
            float angle = _waitingBobTimer * Mathf.PI * 2f * frequency;
            float amplitude = Mathf.Max(0f, _waitingSuccessZoneBobAmplitude);
            float offset = Mathf.Sin(angle) * amplitude;
            lure.SetVisualOffset(new Vector3(0f, offset, 0f));
            _waitingBobOffsetApplied = true;
        }

        private void ResolveRenderers()
        {
            if (_renderersResolved == true)
                return;

            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            _renderersResolved = true;
        }

        private void SetRenderersVisible(bool visible)
        {
            if (_renderersResolved == false)
            {
                ResolveRenderers();
            }

            if (_renderers == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                renderer.enabled = visible;
            }
        }

        private UseLayer GetUseLayer()
        {
            return Character != null ? Character.AnimationController?.AttackLayer : null;
        }

        private void UpdateParabolaString()
        {
            if (_parabolaString == null)
                return;

            if (_activeLureProjectile != null && _activeLureProjectile.LineRendererEndPoint != null && _lureFireTransform != null)
            {
                _parabolaString.SetEndpoints(_lureFireTransform, _activeLureProjectile.LineRendererEndPoint);
            }
            else
            {
                _parabolaString.ClearEndpoints();
            }
        }

        private void HandleActiveLureChanged(bool force = false)
        {
            FishingLureProjectile networkedProjectile = NetworkedActiveLure;
            FishingLureProjectile resolvedProjectile = null;

            if (networkedProjectile != null && networkedProjectile.Object != null && networkedProjectile.Object.IsValid == true)
            {
                resolvedProjectile = networkedProjectile;
            }

            if (force == true || _activeLureProjectile != resolvedProjectile)
            {
                _activeLureProjectile = resolvedProjectile;
                ResetWaitingBobVisuals();
                UpdateParabolaString();
            }
        }
    }
}
