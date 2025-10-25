using Fusion;
using UnityEngine;
using UnityEngine.TextCore.Text;

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

        private float _primaryHoldTime;
        private bool _isPrimaryHeld;
        private bool _castRequested;
        private bool _castActive;
        private bool _waitingForPrimaryRelease;
        private FishingLureProjectile _activeLureProjectile;
        private bool _lureLaunched;

        private void OnEnable()
        {
            if (_parabolaString == null)
            {
                _parabolaString = GetComponent<ParabolaString>();
            }

            _parabolaString?.ClearEndpoints();
        }

        private void OnDisable()
        {
            _parabolaString?.ClearEndpoints();
        }

        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
            // Fishing pole currently has no firing behaviour. Override when casting logic is implemented.
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
            CleanupLure(false);
        }

        internal void NotifyCastThrown()
        {
            // The cast remains active until the animation finishes.
        }

        internal void NotifyCastCompleted()
        {
            _castActive = false;
            _castRequested = false;
            ResetHoldTracking();
            CleanupLure(true);
        }

        internal void NotifyCastCancelled()
        {
            _castActive = false;
            _castRequested = false;
            ResetHoldTracking();
            _waitingForPrimaryRelease = true;
            CleanupLure(true);
        }

        internal void NotifyWaitingPhaseEntered()
        {
            _castActive = false;
            _waitingForPrimaryRelease = false;
            ResetHoldTracking();
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
                UpdateParabolaString();
            });
        }

        internal void OnLureImpacted(FishingLureProjectile projectile, in LagCompensatedHit hit)
        {
            _ = hit;

            if (_castActive == false)
                return;

            if (projectile != null && projectile == _activeLureProjectile)
            {
                _activeLureProjectile = null;
            }

            UseLayer layer = GetUseLayer();

            if (layer?.FishingPoleUseState != null)
            {
                layer.FishingPoleUseState.EnterWaitingPhase(this);
            }

            CleanupLure(false);
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
            UpdateParabolaString();
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
    }
}
