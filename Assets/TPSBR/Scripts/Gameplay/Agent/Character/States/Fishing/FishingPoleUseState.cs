using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class FishingPoleUseState : MixerState
    {
        public  FishingCastParentState CastState => _castState;
        public  FishingWaitingState Waiting => _waiting;
        public  FishingFightingState Fighting => _fighting;
        public  FishingCatchParentState Catch => _catch;

        [SerializeField] private FishingCastParentState _castState;
        [SerializeField] private FishingWaitingState _waiting;
        [SerializeField] private FishingFightingState _fighting;
        [SerializeField] private FishingCatchParentState _catch;

        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.15f;
        [SerializeField] private float _reelSmoothTime = 0.25f;
        [SerializeField] private float _fightingHorizontalRadius = 0.35f;
        [SerializeField] private float _fightingHorizontalSpeed = 0.65f;
        [SerializeField] private float _fightingHorizontalJitterRadius = 0.15f;
        [SerializeField] private float _fightingHorizontalJitterSpeed = 1.2f;
        [SerializeField] private float _fightingVerticalAmplitude = 0.2f;
        [SerializeField] private float _fightingVerticalSpeed = 1.4f;
        [SerializeField] private Vector2 _fightingJumpInterval = new Vector2(2f, 4f);
        [SerializeField] private float _fightingJumpHeight = 0.45f;
        [SerializeField] private float _fightingJumpDuration = 0.6f;

        private FishingPoleWeapon _activeWeapon;
        private bool _isWaiting;
        private bool _awaitingLureImpact;
        private bool _isFighting;
        private bool _isReeling;
        private bool _isCatching;
        private FishingLureProjectile _reelLure;
        private Vector3 _reelDirection;
        private float _reelInitialDistance;
        private Vector3 _reelCurrentOffset;
        private Vector3 _reelTargetOffset;
        private Vector3 _reelOffsetVelocity;
        private Vector3 _reelAnchorPosition;
        private int _reelRequiredHits;
        private int _reelAppliedHits;
        private bool _isReelActive;
        private bool _hasReelAnchorPosition;
        private bool _fightingMotionInitialized;
        private float _fightingMotionTime;
        private Vector3 _fightingMotionOffset;
        private float _fightingHorizontalPhase;
        private float _fightingVerticalPhase;
        private float _fightingJitterPhase;
        private float _fightingHorizontalDirection;
        private float _fightingRadiusMultiplier;
        private float _fightingJumpTimer;
        private float _fightingJumpElapsed;
        private float _fightingNextJumpDelay;
        private bool _fightingJumpActive;

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
            _isReeling = false;
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
            _isReeling = false;
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
            _isReeling = false;
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

            if (weapon.HasStateAuthority == true)
            {
                BeginFightingMotion();
            }
            else
            {
                ResetFightingMotion();
            }

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

            Vector3 anchorPosition = _hasReelAnchorPosition == true ? _reelAnchorPosition : (_reelLure != null ? _reelLure.transform.position - _reelCurrentOffset : Vector3.zero);
            Vector3 finalTargetPosition = anchorPosition + _reelDirection * _reelInitialDistance;

            if (_hasReelAnchorPosition == true && TryGetReelTargetPosition(weapon, out Vector3 resolvedTarget) == true)
            {
                finalTargetPosition = resolvedTarget;

                Vector3 toTarget = finalTargetPosition - anchorPosition;
                float totalDistance = toTarget.magnitude;

                if (totalDistance > 0.001f)
                {
                    _reelDirection = toTarget / totalDistance;
                    _reelInitialDistance = totalDistance;
                }
            }

            if (_hasReelAnchorPosition == true)
            {
                Vector3 desiredPosition = Vector3.Lerp(anchorPosition, finalTargetPosition, normalized);
                _reelTargetOffset = desiredPosition - anchorPosition;
            }
            else
            {
                float targetDistance = _reelInitialDistance * normalized;
                _reelTargetOffset = _reelDirection * targetDistance;
            }

            _isReelActive = true;

            if (_reelAppliedHits >= _reelRequiredHits && _reelRequiredHits > 0)
            {
                SnapReeledLureToTarget(weapon, anchorPosition, finalTargetPosition);
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
            _isReeling = false;
            _isCatching = true;

            if (_fighting != null)
            {
                _fighting.Stop(_blendOutDuration);
                _fighting.ClearActiveWeapon(weapon);
            }

            

            if (_reelLure == null)
            {
                _reelLure = weapon.ActiveLure;
            }

            if (_reelLure != null)
            {
                Vector3 anchorPosition = _hasReelAnchorPosition == true ? _reelAnchorPosition : (_reelLure.transform.position - _reelCurrentOffset);
                Vector3 finalTargetPosition = anchorPosition + _reelDirection * _reelInitialDistance;

                if (TryGetReelTargetPosition(weapon, out Vector3 resolvedTarget) == true)
                {
                    finalTargetPosition = resolvedTarget;
                }

                SnapReeledLureToTarget(weapon, anchorPosition, finalTargetPosition);
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
            _reelAnchorPosition = lurePosition;
            _hasReelAnchorPosition = true;

            Vector3 targetPosition;

            if (TryGetReelTargetPosition(weapon, out Vector3 resolvedTarget) == true)
            {
                targetPosition = resolvedTarget;
            }
            else
            {
                targetPosition = characterTransform.position;
            }

            Vector3 toCharacter = targetPosition - lurePosition;
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
            _reelAnchorPosition = Vector3.zero;
            _reelRequiredHits = 0;
            _reelAppliedHits = 0;
            _isReelActive = false;
            _hasReelAnchorPosition = false;
            ResetFightingMotion();
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

            Vector3 fightingOffset = Vector3.zero;

            if (_isFighting == true)
            {
                fightingOffset = UpdateFightingMotion(deltaTime);
            }
            else if (_fightingMotionInitialized == true || _fightingMotionOffset != Vector3.zero)
            {
                ResetFightingMotion();
            }

            _reelLure.SetVisualOffset(_reelCurrentOffset + fightingOffset);
        }

        private void BeginFightingMotion()
        {
            ResetFightingMotion();

            _fightingMotionInitialized = true;
            _fightingHorizontalPhase = UnityEngine.Random.value * Mathf.PI * 2f;
            _fightingVerticalPhase = UnityEngine.Random.value * Mathf.PI * 2f;
            _fightingJitterPhase = UnityEngine.Random.value * Mathf.PI * 2f;
            _fightingHorizontalDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            _fightingRadiusMultiplier = Mathf.Lerp(0.85f, 1.15f, UnityEngine.Random.value);

            float minDelay = Mathf.Max(0.1f, Mathf.Min(_fightingJumpInterval.x, _fightingJumpInterval.y));
            float maxDelay = Mathf.Max(minDelay, Mathf.Max(_fightingJumpInterval.x, _fightingJumpInterval.y));
            _fightingNextJumpDelay = maxDelay > 0f ? UnityEngine.Random.Range(minDelay, maxDelay) : 0f;
        }

        private void ResetFightingMotion()
        {
            _fightingMotionInitialized = false;
            _fightingMotionTime = 0f;
            _fightingMotionOffset = Vector3.zero;
            _fightingJumpTimer = 0f;
            _fightingJumpElapsed = 0f;
            _fightingJumpActive = false;
            _fightingNextJumpDelay = 0f;
        }

        private Vector3 UpdateFightingMotion(float deltaTime)
        {
            if (_fightingMotionInitialized == false)
            {
                BeginFightingMotion();
            }

            if (deltaTime <= 0f)
            {
                deltaTime = Time.deltaTime;
            }

            _fightingMotionTime += deltaTime;

            float radius = Mathf.Max(0f, _fightingHorizontalRadius) * (_fightingRadiusMultiplier != 0f ? _fightingRadiusMultiplier : 1f);
            float speed = Mathf.Max(0f, _fightingHorizontalSpeed);
            float horizontalAngle = _fightingMotionTime * speed * _fightingHorizontalDirection + _fightingHorizontalPhase;

            float horizontalX = 0f;
            float horizontalZ = 0f;

            if (radius > 0f && speed > 0f)
            {
                horizontalX = Mathf.Cos(horizontalAngle) * radius;
                horizontalZ = Mathf.Sin(horizontalAngle) * radius;
            }

            float jitterRadius = Mathf.Max(0f, _fightingHorizontalJitterRadius);
            float jitterSpeed = Mathf.Max(0f, _fightingHorizontalJitterSpeed);

            if (jitterRadius > 0f && jitterSpeed > 0f)
            {
                float jitterAngle = _fightingMotionTime * jitterSpeed + _fightingJitterPhase;
                horizontalX += Mathf.Sin(jitterAngle) * jitterRadius;
                horizontalZ += Mathf.Cos(jitterAngle * 0.75f) * jitterRadius;
            }

            float vertical = 0f;
            float verticalAmplitude = Mathf.Max(0f, _fightingVerticalAmplitude);
            float verticalSpeed = Mathf.Max(0f, _fightingVerticalSpeed);

            if (verticalAmplitude > 0f && verticalSpeed > 0f)
            {
                vertical = Mathf.Sin(_fightingMotionTime * verticalSpeed + _fightingVerticalPhase) * verticalAmplitude;
            }

            float jumpHeight = Mathf.Max(0f, _fightingJumpHeight);

            if (jumpHeight > 0f)
            {
                float jumpDuration = Mathf.Max(0.01f, _fightingJumpDuration);
                float minDelay = Mathf.Max(0.1f, Mathf.Min(_fightingJumpInterval.x, _fightingJumpInterval.y));
                float maxDelay = Mathf.Max(minDelay, Mathf.Max(_fightingJumpInterval.x, _fightingJumpInterval.y));

                if (_fightingJumpActive == false && maxDelay > 0f)
                {
                    _fightingJumpTimer += deltaTime;

                    if (_fightingNextJumpDelay <= 0f)
                    {
                        _fightingNextJumpDelay = UnityEngine.Random.Range(minDelay, maxDelay);
                    }

                    if (_fightingJumpTimer >= _fightingNextJumpDelay)
                    {
                        _fightingJumpActive = true;
                        _fightingJumpTimer = 0f;
                        _fightingJumpElapsed = 0f;
                    }
                }

                if (_fightingJumpActive == true)
                {
                    _fightingJumpElapsed += deltaTime;
                    float normalized = Mathf.Clamp01(_fightingJumpElapsed / jumpDuration);
                    vertical += Mathf.Sin(normalized * Mathf.PI) * jumpHeight;

                    if (normalized >= 1f)
                    {
                        _fightingJumpActive = false;
                        _fightingJumpElapsed = 0f;
                        _fightingNextJumpDelay = maxDelay > 0f ? UnityEngine.Random.Range(minDelay, maxDelay) : 0f;
                    }
                }
            }

            _fightingMotionOffset = new Vector3(horizontalX, vertical, horizontalZ);
            return _fightingMotionOffset;
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
            _isFighting = false;
            _isReeling = false;
            _isCatching = false;

            if (_activeWeapon != null)
            {
                _activeWeapon.NotifyCastCompleted();
            }

            // If the lure is still in flight, keep the active weapon reference so impact
            // callbacks can transition into the waiting phase and update the lifecycle.
            if (_awaitingLureImpact == true)
                return;

            _awaitingLureImpact = false;

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
            _isReeling = false;
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
            _isReeling = false;
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

        private bool TryGetReelTargetPosition(FishingPoleWeapon weapon, out Vector3 targetPosition)
        {
            targetPosition = default;

            if (weapon == null)
                return false;

            Character character = weapon.Character;

            if (character == null)
                return false;

            CharacterView view = character.ThirdPersonView;
            Transform fireTransform = view != null ? view.FireTransform : null;

            if (fireTransform != null)
            {
                targetPosition = fireTransform.position;
                return true;
            }

            targetPosition = character.transform.position;
            return true;
        }

        private void SnapReeledLureToTarget(FishingPoleWeapon weapon, Vector3 anchorPosition, Vector3 targetPosition)
        {
            if (weapon == null)
                return;

            if (_reelLure == null)
            {
                _reelLure = weapon.ActiveLure;

                if (_reelLure == null)
                    return;
            }

            if (_hasReelAnchorPosition == false)
            {
                anchorPosition = _reelLure.transform.position - _reelCurrentOffset;
            }

            if (TryGetReelTargetPosition(weapon, out Vector3 resolvedTarget) == true)
            {
                targetPosition = resolvedTarget;
            }

            _reelAnchorPosition = anchorPosition;
            _hasReelAnchorPosition = true;

            Vector3 offset = targetPosition - anchorPosition;

            float distance = offset.magnitude;

            if (distance > 0.001f)
            {
                _reelDirection = offset / distance;
                _reelInitialDistance = distance;
            }

            _reelTargetOffset = offset;
            _reelCurrentOffset = offset;
            _reelOffsetVelocity = Vector3.zero;
            _isReelActive = true;

            _reelLure.SetVisualOffset(_reelCurrentOffset);
        }
    }
}
