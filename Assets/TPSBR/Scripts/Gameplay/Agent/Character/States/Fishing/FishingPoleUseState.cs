using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class FishingPoleUseState : MixerState
    {
        [SerializeField] private FishingCastParentState _castState;
        [SerializeField] private FishingWaitingState _waiting;
        [SerializeField] private FishingFightingState _fighting;
        [SerializeField] private FishingCatchParentState _catch;

        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.15f;
        [SerializeField] private float _reelSmoothTime = 0.25f;

        private FishingPoleWeapon _activeWeapon;
        private bool _isWaiting;
        private bool _awaitingLureImpact;
        private bool _isFighting;
        private bool _isCatching;
        private FishingLureProjectile _reelLure;
        private Vector3 _reelDirection;
        private float _reelInitialDistance;
        private Vector3 _reelCurrentOffset;
        private Vector3 _reelTargetOffset;
        private Vector3 _reelOffsetVelocity;
        private int _reelRequiredHits;
        private int _reelAppliedHits;
        private bool _isReelActive;

        public bool IsCatchLoopActive => _catch != null && _catch.IsLoopActive == true;

        public bool BeginCast(FishingPoleWeapon weapon)
        {
            if (weapon == null || _castState == null)
                return false;

            if (_activeWeapon != null && _activeWeapon != weapon)
                return false;

            _activeWeapon = weapon;
            _isWaiting = false;
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;
            ResetReeling();

            _castState.SetActiveWeapon(weapon);
            _waiting?.SetActiveWeapon(weapon);
            _fighting?.SetActiveWeapon(weapon);
            _catch?.SetActiveWeapon(weapon);

            _castState.PlayBegin(_blendInDuration);
            Activate(_blendInDuration);

            _activeWeapon.NotifyCastStarted();

            return true;
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            float deltaTime = (_activeWeapon != null && _activeWeapon.Runner != null) ? _activeWeapon.Runner.DeltaTime : Time.fixedDeltaTime;

            if (_activeWeapon != null && _activeWeapon.HasStateAuthority == true)
            {
                UpdateReelMovement(deltaTime);
            }

            if (_activeWeapon == null || _castState == null)
            {
                if (_activeWeapon == null)
                {
                    ResetReeling();
                }

                return;
            }

            Agent agent = _activeWeapon.Character != null ? _activeWeapon.Character.Agent : null;

            if (agent == null || agent.AgentInput == null)
            {
                CancelCast();
                return;
            }

            AgentInput agentInput = agent.AgentInput;

            bool beginActive = _castState.IsBeginActive;
            bool throwActive = _castState.IsThrowActive;
            bool waitingActive = _isWaiting == true && _waiting != null && _waiting.IsActive(true) == true;
            bool fightingActive = _isFighting == true && _fighting != null && _fighting.IsActive(true) == true;
            bool catchActive = _isCatching == true && _catch != null && _catch.IsActive(true) == true;

            if (beginActive == true)
            {
                bool primaryReleased = agentInput.WasDeactivated(EGameplayInputAction.Attack);
                bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                          agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                if (secondaryActivated == true)
                {
                    CancelCast();
                    return;
                }

                if (primaryReleased == true)
                {
                    _castState.PlayThrow(_blendInDuration);
                    _activeWeapon.NotifyCastThrown();
                    _awaitingLureImpact = true;
                }
            }
            else if (throwActive == true)
            {
                bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                          agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                if (secondaryActivated == true)
                {
                    _awaitingLureImpact = false;
                    CancelCast();
                    return;
                }
            }
            else if (waitingActive == true)
            {
                bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                          agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                if (secondaryActivated == true)
                {
                    CancelCast();
                }
            }
            else if (fightingActive == true)
            {
                bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                          agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                if (secondaryActivated == true)
                {
                    CancelCast();
                }
            }
            else if (catchActive == true)
            {
                if (_catch != null && _catch.IsLoopActive == true)
                {
                    bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                              agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                    if (secondaryActivated == true)
                    {
                        _catch.PlayEnd(_blendInDuration);
                    }
                }

                if (_catch != null && _catch.HasCompletedSequence == true)
                {
                    CompleteCatch();
                    return;
                }
            }
            else
            {
                if (_awaitingLureImpact == true)
                    return;

                // No active cast clips, ensure the layer is reset.
                CompleteCast();
            }
        }

        protected override void OnInterpolate()
        {
            base.OnInterpolate();

            if (_activeWeapon == null)
                return;

            if (_activeWeapon.HasStateAuthority == true)
                return;

            UpdateReelMovement(Time.deltaTime);
        }

        internal void EnterWaitingPhase(FishingPoleWeapon weapon)
        {
            if (_waiting == null || weapon == null || _activeWeapon != weapon)
                return;

            if (_isWaiting == true)
                return;

            ResetReeling();

            _isWaiting = true;
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;

            _waiting.SetActiveWeapon(weapon);
            _waiting.Play(_blendInDuration);

            weapon.NotifyWaitingPhaseEntered();

            if (_castState != null)
            {
                _castState.Stop(_blendOutDuration);
            }

            Activate(_blendInDuration);
        }

        internal void EnterFightingPhase(FishingPoleWeapon weapon)
        {
            if (_fighting == null || weapon == null || _activeWeapon != weapon)
                return;

            if (_isFighting == true)
                return;

            _isWaiting = false;
            _isFighting = true;
            _isCatching = false;

            if (_waiting != null)
            {
                _waiting.Stop(_blendOutDuration);
                _waiting.ClearActiveWeapon(weapon);
            }

            _fighting.SetActiveWeapon(weapon);
            _fighting.Play(_blendInDuration);

            ResetReeling();
            BeginReeling(weapon);

            weapon.NotifyFightingPhaseEntered();

            Activate(_blendInDuration);
        }

        internal void UpdateFightingMinigameProgress(FishingPoleWeapon weapon, int successHits, int requiredHits)
        {
            if (weapon == null || _activeWeapon != weapon)
                return;

            if (_isFighting == false)
                return;

            if (_isReelActive == false)
            {
                BeginReeling(weapon);
            }

            if (_reelLure == null)
            {
                _reelLure = weapon.ActiveLure;
            }

            if (_isReelActive == false || _reelInitialDistance <= 0f || _reelLure == null)
                return;

            int clampedRequired = Mathf.Max(1, requiredHits);
            bool hasNewRequirement = clampedRequired > _reelRequiredHits;
            _reelRequiredHits = Mathf.Max(_reelRequiredHits, clampedRequired);

            successHits = Mathf.Clamp(successHits, 0, _reelRequiredHits);

            bool hasNewProgress = successHits > _reelAppliedHits;

            if (hasNewProgress == false && hasNewRequirement == false)
                return;

            _reelAppliedHits = Mathf.Min(successHits, _reelRequiredHits);

            float normalized = _reelRequiredHits > 0 ? (float)_reelAppliedHits / _reelRequiredHits : 0f;
            normalized = Mathf.Clamp01(normalized);

            float targetDistance = _reelInitialDistance * normalized;
            _reelTargetOffset = _reelDirection * targetDistance;
            _isReelActive = true;

            if (_reelAppliedHits >= _reelRequiredHits && _reelRequiredHits > 0)
            {
                _reelTargetOffset = _reelDirection * _reelInitialDistance;
                _reelCurrentOffset = _reelTargetOffset;
                _reelOffsetVelocity = Vector3.zero;

                if (_reelLure != null)
                {
                    _reelLure.SetVisualOffset(_reelCurrentOffset);
                }
            }
        }

        internal void EnterCatchPhase(FishingPoleWeapon weapon)
        {
            if (_catch == null || weapon == null || _activeWeapon != weapon)
                return;

            if (_isCatching == true)
                return;

            _isWaiting = false;
            _isFighting = false;
            _isCatching = true;

            if (_fighting != null)
            {
                _fighting.Stop(_blendOutDuration);
                _fighting.ClearActiveWeapon(weapon);
            }

            ResetReeling(clearVisualOffset: false);

            _catch.SetActiveWeapon(weapon);
            _catch.StartCatch(_blendInDuration);

            Activate(_blendInDuration);
        }

        internal bool TryInterruptWaitingForNewCast(FishingPoleWeapon weapon)
        {
            if (_waiting == null || weapon == null)
                return false;

            if (_activeWeapon == null || _activeWeapon != weapon)
                return false;

            if (_isWaiting == false)
                return false;

            CompleteCast();

            return true;
        }

        internal bool TryCancelActiveCast(FishingPoleWeapon weapon)
        {
            if (weapon == null)
                return false;

            if (_activeWeapon == null || _activeWeapon != weapon)
                return false;

            CancelCast();

            return true;
        }

        private void BeginReeling(FishingPoleWeapon weapon)
        {
            if (weapon == null)
                return;

            FishingLureProjectile lure = weapon.ActiveLure;
            Transform characterTransform = weapon.Character != null ? weapon.Character.transform : null;

            if (lure == null || characterTransform == null)
                return;

            Vector3 lurePosition = lure.transform.position;
            Vector3 characterPosition = characterTransform.position;
            Vector3 toCharacter = characterPosition - lurePosition;
            float distance = toCharacter.magnitude;

            if (distance <= 0.01f)
                return;

            _reelLure = lure;
            _reelDirection = toCharacter / distance;
            _reelInitialDistance = distance;
            _reelCurrentOffset = Vector3.zero;
            _reelTargetOffset = Vector3.zero;
            _reelOffsetVelocity = Vector3.zero;
            _reelRequiredHits = 0;
            _reelAppliedHits = 0;
            _isReelActive = true;

            _reelLure.SetVisualOffset(Vector3.zero);
        }

        private void ResetReeling(bool clearVisualOffset = true)
        {
            if (_reelLure != null && clearVisualOffset == true)
            {
                _reelLure.SetVisualOffset(Vector3.zero);
            }

            _reelLure = null;
            _reelDirection = Vector3.zero;
            _reelInitialDistance = 0f;
            _reelCurrentOffset = Vector3.zero;
            _reelTargetOffset = Vector3.zero;
            _reelOffsetVelocity = Vector3.zero;
            _reelRequiredHits = 0;
            _reelAppliedHits = 0;
            _isReelActive = false;
        }

        private void UpdateReelMovement(float deltaTime)
        {
            if (_isReelActive == false)
                return;

            if (_activeWeapon == null)
            {
                ResetReeling();
                return;
            }

            if (_reelLure == null)
            {
                _reelLure = _activeWeapon.ActiveLure;

                if (_reelLure == null)
                {
                    ResetReeling();
                    return;
                }
            }

            if (deltaTime <= 0f)
            {
                deltaTime = Time.deltaTime;
            }

            float smoothTime = Mathf.Max(0.01f, _reelSmoothTime);
            _reelCurrentOffset = Vector3.SmoothDamp(_reelCurrentOffset, _reelTargetOffset, ref _reelOffsetVelocity, smoothTime, float.PositiveInfinity, deltaTime);

            if ((_reelCurrentOffset - _reelTargetOffset).sqrMagnitude <= 0.0001f)
            {
                _reelCurrentOffset = _reelTargetOffset;
            }

            _reelLure.SetVisualOffset(_reelCurrentOffset);
        }

        private void CompleteCast()
        {
            ResetReeling();

            if (_castState != null)
            {
                _castState.Stop(_blendOutDuration);
            }

            if (_waiting != null)
            {
                _waiting.Stop(_blendOutDuration);
                _waiting.ClearActiveWeapon(_activeWeapon);
            }

            if (_fighting != null)
            {
                _fighting.Stop(_blendOutDuration);
                _fighting.ClearActiveWeapon(_activeWeapon);
            }

            if (_catch != null)
            {
                _catch.Stop(_blendOutDuration);
                _catch.ClearActiveWeapon(_activeWeapon);
            }

            _isWaiting = false;
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;

            if (_activeWeapon != null)
            {
                _activeWeapon.NotifyCastCompleted();
            }

            Finish();
        }

        private void CancelCast()
        {
            ResetReeling();

            if (_castState != null)
            {
                _castState.Stop(_blendOutDuration);
            }

            if (_waiting != null)
            {
                _waiting.Stop(_blendOutDuration);
                _waiting.ClearActiveWeapon(_activeWeapon);
            }

            if (_fighting != null)
            {
                _fighting.Stop(_blendOutDuration);
                _fighting.ClearActiveWeapon(_activeWeapon);
            }

            if (_catch != null)
            {
                _catch.Stop(_blendOutDuration);
                _catch.ClearActiveWeapon(_activeWeapon);
            }

            _isWaiting = false;
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;

            if (_activeWeapon != null)
            {
                _activeWeapon.NotifyCastCancelled();
            }

            Finish();
        }

        private void Finish()
        {
            ResetReeling();

            if (_castState != null && _activeWeapon != null)
            {
                _castState.ClearActiveWeapon(_activeWeapon);
            }

            if (_waiting != null && _activeWeapon != null)
            {
                _waiting.ClearActiveWeapon(_activeWeapon);
            }

            if (_fighting != null && _activeWeapon != null)
            {
                _fighting.ClearActiveWeapon(_activeWeapon);
            }

            if (_catch != null && _activeWeapon != null)
            {
                _catch.ClearActiveWeapon(_activeWeapon);
            }

            _activeWeapon = null;
            _isWaiting = false;
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;

            if (IsActive(true) == true)
            {
                Deactivate(_blendOutDuration);
            }
        }

        private void CompleteCatch()
        {
            _isCatching = false;

            CompleteCast();
        }
    }
}
