using System;
using UnityEngine;
using UnityEngine.Profiling;
using Fusion;
using Fusion.Addons.KCC;

namespace TPSBR
{
    [DefaultExecutionOrder(-5)]
    public sealed class Agent : ContextBehaviour, ISortedUpdate
    {
        // PUBLIC METHODS

        public bool IsObserved => Context != null && Context.ObservedAgent == this;

        public AgentInput AgentInput => _agentInput;
        public Interactions Interactions => _interactions;
        public Character Character => _character;
        public Inventory Inventory => _inventory;
        public Health Health => _health;
        public AgentMana Mana => _mana;
        public AgentStamina Stamina => _stamina;
        public AgentSenses Senses => _senses;
        public AgentVFX Effects => _agentVFX;
        public AgentInterestView InterestView => _interestView;
        public Stats Stats => _stats;
        public Professions Professions => _professions;
        public BuffSystem BuffSystem => _buffSystem;
        public event Action<SceneRef, SceneSwitchMode, bool, string> SceneSwitchRequestProcessed;

        // PRIVATE MEMBERS

        [SerializeField] private float _jumpPower;
        [SerializeField] private float _topCameraAngleLimit;
        [SerializeField] private float _bottomCameraAngleLimit;
        [SerializeField] private GameObject _visualRoot;

        [Header("Fall Damage")] [SerializeField]
        private float _minFallDamage = 5f;

        [SerializeField] private float _maxFallDamage = 200f;
        [SerializeField] private float _maxFallDamageVelocity = 20f;
        [SerializeField] private float _minFallDamageVelocity = 5f;

        private AgentInput _agentInput;
        private Interactions _interactions;
        private AgentFootsteps _footsteps;
        private Character _character;
        private Inventory _inventory;
        private AgentSenses _senses;
        private Health _health;
        private AgentMana _mana;
        private AgentStamina _stamina;
        private AgentVFX _agentVFX;
        private AgentInterestView _interestView;
        private SortedUpdateInvoker _sortedUpdateInvoker;
        private Quaternion _cachedLookRotation;
        private Quaternion _cachedPitchRotation;
        private Stats _stats;
        private Professions _professions;
        private EquipmentVisualsManager _equipmentVisuals;
        private AgentNameplate _agentNameplate;
        private BuffSystem _buffSystem;

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            name = Object.InputAuthority.ToString();

            _sortedUpdateInvoker = Runner.GetSingleton<SortedUpdateInvoker>();

            _visualRoot.SetActive(true);

            _character.OnSpawned(this);
            _health.OnSpawned(this);
            _mana.OnSpawned(this);
            _stamina?.OnSpawned(this);
            _agentVFX.OnSpawned(this);
            _stats.OnSpawned(this);
            _professions.OnSpawned(this);
            _equipmentVisuals?.Initialize(_inventory);
            _agentNameplate?.OnSpawned(this, Runner.Mode == SimulationModes.Server);
            if (ApplicationSettings.IsStrippedBatch == true)
            {
                gameObject.SetActive(false);

                if (ApplicationSettings.GenerateInput == true)
                {
                    NetworkEvents networkEvents = Runner.GetComponent<NetworkEvents>();
                    networkEvents.OnInput.RemoveListener(GenerateRandomInput);
                    networkEvents.OnInput.AddListener(GenerateRandomInput);
                }
            }

            return;

            void GenerateRandomInput(NetworkRunner runner, NetworkInput networkInput)
            {
                // Used for batch testing

                GameplayInput gameplayInput = new GameplayInput();
                gameplayInput.MoveDirection = new Vector2(UnityEngine.Random.value * 2.0f - 1.0f,
                    UnityEngine.Random.value > 0.25f ? 1.0f : -1.0f).normalized;
                gameplayInput.LookRotationDelta = new Vector2(UnityEngine.Random.value * 2.0f - 1.0f,
                    UnityEngine.Random.value * 2.0f - 1.0f);
                gameplayInput.Jump = UnityEngine.Random.value > 0.99f;
                gameplayInput.Attack = UnityEngine.Random.value > 0.99f;
                gameplayInput.Interact = UnityEngine.Random.value > 0.99f;
                gameplayInput.Weapon =
                    (byte)(UnityEngine.Random.value > 0.99f ? (UnityEngine.Random.value > 0.25f ? 2 : 1) : 0);

                networkInput.Set(gameplayInput);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_inventory != null) _inventory.OnDespawned();
            if (_health != null) _health.OnDespawned();
            if (_mana != null)  _mana.OnDespawned();
            if (_stamina != null) _stamina.OnDespawned();
            if (_agentVFX != null) _agentVFX.OnDespawned();
            if (_stats != null) _stats.OnDespawned();
            if (_professions != null) _professions.OnDespawned();
            if (_agentNameplate != null) _agentNameplate.OnDespawned();

            _equipmentVisuals?.Initialize(null);
        }

        public void EarlyFixedUpdateNetwork()
        {
            Profiler.BeginSample($"{nameof(Agent)}(Early)");

            ProcessFixedInput();

            _inventory.OnFixedUpdate();
            _character.OnFixedUpdate();

            Profiler.EndSample();
        }

        public override void FixedUpdateNetwork()
        {
            Profiler.BeginSample($"{nameof(Agent)}(Regular)");

            // Performance optimization, unnecessary euler call
            Quaternion currentLookRotation = _character.CharacterController.FixedData.LookRotation;
            if (_cachedLookRotation.ComponentEquals(currentLookRotation) == false)
            {
                _cachedLookRotation = currentLookRotation;
                _cachedPitchRotation = Quaternion.Euler(_character.CharacterController.FixedData.LookPitch, 0.0f, 0.0f);
            }

            _character.GetCameraHandle().transform.localRotation = _cachedPitchRotation;

            CheckFallDamage();

            if (_health.IsAlive == true)
            {
                float sortOrder = _agentInput.FixedInput.LocalAlpha;
                if (sortOrder <= 0.0f)
                {
                    // Default LocalAlpha value results in update callback being executed last.
                    sortOrder = 1.0f;
                }

                // Schedule update to process render-accurate shooting.
                _sortedUpdateInvoker.ScheduleSortedUpdate(this, sortOrder);

                if (Runner.IsServer == true)
                {
                    _interestView.SetPlayerInfo(_character.CharacterController.Transform, _character.GetCameraHandle());
                }
            }

            _health.OnFixedUpdate();

            Profiler.EndSample();
        }

        public void EarlyRender()
        {
            if (HasInputAuthority == true)
            {
                ProcessRenderInput();
            }

            _character.OnRender();
        }

        public bool RequestSceneSwitch(SceneRef sceneRef, SceneSwitchMode mode, LocalPhysicsMode localPhysicsMode, bool setActiveOnLoad)
        {
            if (Runner == null)
                return false;

            if (Object == null || Object.HasInputAuthority == false)
                return false;

            RPC_RequestSceneSwitch(sceneRef, mode, localPhysicsMode, setActiveOnLoad);
            return true;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = false)]
        private void RPC_RequestSceneSwitch(SceneRef sceneRef, SceneSwitchMode mode, LocalPhysicsMode physicsMode, bool setActiveOnLoad, RpcInfo rpcInfo = default)
        {
            if (Runner == null)
                return;

            if (sceneRef.IsValid == false)
            {
                RPC_NotifySceneSwitchProcessed(sceneRef, mode, false, "Invalid scene reference.");
                return;
            }

            if (RunnerAdditiveSceneManager.IsSceneOperationInProgress(Runner, sceneRef) == true)
            {
                RPC_NotifySceneSwitchProcessed(sceneRef, mode, false, "Another scene operation is already in progress.");
                return;
            }

            SceneSwitchMode desiredMode = mode;

            if (mode == SceneSwitchMode.Toggle)
            {
                desiredMode = RunnerAdditiveSceneManager.IsSceneLoaded(Runner, sceneRef) ? SceneSwitchMode.Unload : SceneSwitchMode.Load;
            }

            if (desiredMode == SceneSwitchMode.Unload && RunnerAdditiveSceneManager.IsSceneLoaded(Runner, sceneRef) == false)
            {
                RPC_NotifySceneSwitchProcessed(sceneRef, desiredMode, true, null);
                return;
            }

            try
            {
                NetworkSceneAsyncOp operation;

                switch (desiredMode)
                {
                    case SceneSwitchMode.Load:
                        operation = RunnerAdditiveSceneManager.LoadAdditiveScene(Runner, sceneRef, physicsMode, setActiveOnLoad);
                        break;

                    case SceneSwitchMode.Unload:
                        operation = RunnerAdditiveSceneManager.UnloadAdditiveScene(Runner, sceneRef);
                        break;

                    default:
                        RPC_NotifySceneSwitchProcessed(sceneRef, mode, false, "Unsupported scene switch mode.");
                        return;
                }

                if (operation.IsValid)
                {
                    operation.AddOnCompleted(result =>
                    {
                        bool success = result.Error == null;
                        string message = success ? null : result.Error.Message;
                        RPC_NotifySceneSwitchProcessed(sceneRef, desiredMode, success, message);
                    });
                }
                else
                {
                    RPC_NotifySceneSwitchProcessed(sceneRef, desiredMode, false, "Scene operation is invalid.");
                }
            }
            catch (Exception exception)
            {
                RPC_NotifySceneSwitchProcessed(sceneRef, desiredMode, false, exception.Message);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_NotifySceneSwitchProcessed(SceneRef sceneRef, SceneSwitchMode mode, bool success, string message, RpcInfo rpcInfo = default)
        {
            SceneSwitchRequestProcessed?.Invoke(sceneRef, mode, success, message);
        }

        public override void Render()
        {
            if (HasInputAuthority == true || IsObserved == true)
            {
                // Performance optimization, unnecessary euler call
                Quaternion currentLookRotation = _character.CharacterController.RenderData.LookRotation;
                if (_cachedLookRotation.ComponentEquals(currentLookRotation) == false)
                {
                    _cachedLookRotation = currentLookRotation;
                    _cachedPitchRotation =
                        Quaternion.Euler(_character.CharacterController.RenderData.LookPitch, 0.0f, 0.0f);
                }

                _character.GetCameraHandle().transform.localRotation = _cachedPitchRotation;
            }

            _character.OnAgentRender();
            _footsteps.OnAgentRender();


            if(Object.HasInputAuthority == false)
                _agentNameplate.OnAgentRender();
        }

        // ISortedUpdate INTERFACE

        void ISortedUpdate.SortedUpdate()
        {
            // This method execution is sorted by LocalAlpha property passed in input and preserves realtime order of input actions.

            bool attackWasActivated = _agentInput.WasActivated(EGameplayInputAction.Attack);
            bool interactWasActivated = _agentInput.WasActivated(EGameplayInputAction.Interact);

            TryUseItem(attackWasActivated, _agentInput.FixedInput.Attack);

            _interactions.TryInteract(interactWasActivated, _agentInput.FixedInput.Interact);
        }

        // MonoBehaviour INTERFACE

        private void Awake()
        {
            _agentInput = GetComponent<AgentInput>();
            _interactions = GetComponent<Interactions>();
            _footsteps = GetComponent<AgentFootsteps>();
            _character = GetComponent<Character>();
            _inventory = GetComponent<Inventory>();
            _health = GetComponent<Health>();
            _mana = GetComponent<AgentMana>();
            _stamina = GetComponent<AgentStamina>();
            _agentVFX = GetComponent<AgentVFX>();
            _senses = GetComponent<AgentSenses>();
            _interestView = GetComponent<AgentInterestView>();
            _stats = GetComponent<Stats>();
            _professions = GetComponent<Professions>();
            _buffSystem = GetComponent<BuffSystem>();

            _equipmentVisuals = GetComponentInChildren<EquipmentVisualsManager>();
            _agentNameplate = GetComponentInChildren<AgentNameplate>();
        }

        // PRIVATE METHODS

        private void ProcessFixedInput()
        {
            KCC kcc = _character.CharacterController;
            KCCData kccFixedData = kcc.FixedData;

            GameplayInput input = default;

            if (_health.IsAlive == true)
            {
                input = _agentInput.FixedInput;
            }


            if (_agentInput.WasActivated(EGameplayInputAction.Jump, input) == true &&
                _character.AnimationController.CanJump() == true)
            {
                kcc.Jump(Vector3.up * _jumpPower);
            }

            SetLookRotation(kccFixedData, input.LookRotationDelta );

            kcc.SetInputDirection(input.MoveDirection.IsZero() == true
                ? Vector3.zero
                : kcc.FixedData.TransformRotation * input.MoveDirection.X0Y());

            if (input.Weapon == AgentInput.WeaponDeselectValue)
            {
                if (_character.AnimationController.CanSwitchWeapons(false) == true)
                {
                    _inventory.DisarmCurrentWeapon();
                }
            }
            else if (input.Weapon > 0 &&
                     _character.AnimationController.CanSwitchWeapons(true) ==
                     true) //&& _inventory.SwitchWeapon(input.Weapon - 1) == true)
            {
                //_inventory.SetCurrentWeapon(input.Weapon - 1);
                _inventory.SwitchWeapon(input.Weapon - 1);
                //_character.AnimationController.SwitchWeapons();
            }

            if (_agentInput.WasActivated(EGameplayInputAction.FishingPoleToggle, input) == true)
            {
                _inventory.ToggleFishingPole();
            }

            _agentInput.SetFixedInput(input, false);
        }

        private void ProcessRenderInput()
        {
            KCC kcc = _character.CharacterController;
            KCCData kccFixedData = kcc.FixedData;

            GameplayInput input = default;

            if (_health.IsAlive == true)
            {
                input = _agentInput.RenderInput;

                var accumulatedInput = _agentInput.AccumulatedInput;

                input.LookRotationDelta = accumulatedInput.LookRotationDelta;
            }


            SetLookRotation(kccFixedData, input.LookRotationDelta);

            kcc.SetInputDirection(input.MoveDirection.IsZero() == true
                ? Vector3.zero
                : kcc.RenderData.TransformRotation * input.MoveDirection.X0Y());

            if (_agentInput.WasActivated(EGameplayInputAction.Jump, input) == true &&
                _character.AnimationController.CanJump() == true)
            {
                kcc.Jump(Vector3.up * _jumpPower);
            }
        }

        private void TryUseItem(bool attack, bool hold)
        {
            Weapon currentWeapon = _inventory.CurrentWeapon;

            if (currentWeapon != null)
            {
                bool attackReleased = _agentInput.WasDeactivated(EGameplayInputAction.Attack);
                bool heavyAttackActivated = _agentInput.WasActivated(EGameplayInputAction.HeavyAttack);
                bool blockHeld = _agentInput.FixedInput.Block;

                if (currentWeapon is StaffWeapon staffWeapon)
                {
                    staffWeapon.ApplyExtendedInput(heavyAttackActivated, blockHeld);
                }

                // Evaluate how the weapon wants to handle the current input snapshot.
                WeaponUseRequest useRequest = currentWeapon.EvaluateUse(attack, hold, attackReleased);

                if (useRequest.ShouldUse == false)
                {
                    return;
                }

                if (_character.AnimationController.StartUseItem(currentWeapon, useRequest) == false)
                {
                    return;
                }

                currentWeapon.OnUseStarted(useRequest);

                if (useRequest.FireImmediately == true && _inventory.Fire() == true)
                {
                    _health.ResetRegenDelay();

                    if (Runner.IsServer == true)
                    {
                        PlayerRef inputAuthority = Object.InputAuthority;
                        if (inputAuthority.IsRealPlayer == true)
                        {
                            _interestView.UpdateShootInterestTargets();
                        }
                    }
                }

                return;
            }

            // TODO: Handle consumable usage (trigger animation, apply effects, sync to network).
        }

        private bool CanAim(KCCData kccData)
        {
            if (kccData.IsGrounded == false)
                return false;

            return _inventory.CanAim();
        }

        private void SetLookRotation(KCCData kccData, Vector2 lookRotationDelta)
        {
            Vector2 baseLookRotation = kccData.GetLookRotation(true, true) - kccData.Recoil;
            Vector2 recoilReduction = Vector2.zero;

            lookRotationDelta -= recoilReduction;

            lookRotationDelta.x =
                Mathf.Clamp(baseLookRotation.x + lookRotationDelta.x, -_topCameraAngleLimit, _bottomCameraAngleLimit) -
                baseLookRotation.x;

            _character.CharacterController.SetLookRotation(baseLookRotation  + lookRotationDelta);

            _character.AnimationController.Turn(lookRotationDelta.y);
        }

        private void CheckFallDamage()
        {
            if (IsProxy == true)
                return;

            if (_health.IsAlive == false)
                return;

            var kccData = _character.CharacterController.Data;

            if (kccData.IsGrounded == false || kccData.WasGrounded == true)
                return;

            float fallVelocity = -kccData.DesiredVelocity.y;
            for (int i = 1; i < 3; ++i)
            {
                var historyData = _character.CharacterController.GetHistoryData(kccData.Tick - i);
                if (historyData != null)
                {
                    fallVelocity = Mathf.Max(fallVelocity, -historyData.DesiredVelocity.y);
                }
            }

            if (fallVelocity < 0f)
                return;

            float damage = MathUtility.Map(_minFallDamageVelocity, _maxFallDamageVelocity, 0f, _maxFallDamage,
                fallVelocity);

            if (damage <= _minFallDamage)
                return;

            var hitData = new HitData
            {
                Action = EHitAction.Damage,
                Amount = damage,
                Position = transform.position,
                Normal = Vector3.up,
                Direction = -Vector3.up,
                InstigatorRef = Object.InputAuthority,
                Instigator = _health,
                Target = _health,
                HitType = EHitType.Suicide,
            };

            (_health as IHitTarget).ProcessHit(ref hitData);
        }

        private void OnCullingUpdated(bool isCulled)
        {
            bool isActive = isCulled == false;

            // Show/hide the game object based on AoI (Area of Interest)

            _visualRoot.SetActive(isActive);

            if (_character.CharacterController.Collider != null)
            {
                _character.CharacterController.Collider.enabled = isActive;
            }
        }
    }
}