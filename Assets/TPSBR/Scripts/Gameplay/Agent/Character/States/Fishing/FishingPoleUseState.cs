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
        private bool _isCatching;
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
            _isCatching = false;

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

            if (_activeWeapon == null || _castState == null)
            {
                if (_activeWeapon == null)
                {
                    // No active weapon, nothing to update.
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

        internal void EnterWaitingPhase(FishingPoleWeapon weapon)
        {
            if (_waiting == null || weapon == null || _activeWeapon != weapon)
                return;

            if (_isWaiting == true)
                return;

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
            if (weapon == null || _activeWeapon != weapon || _isFighting == false)
                return;
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
