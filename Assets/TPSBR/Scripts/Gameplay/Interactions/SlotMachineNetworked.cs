using Fusion;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public sealed class SlotMachineNetworked : GamblingMachine
    {
        public const int MinWager = 5;
        public const int MaxWager = 100;
        public const int WagerStep = 5;

        [Header("Payouts")]
        [SerializeField]
        private int _pairMatchPayout = 10;
        [SerializeField]
        private int _jackpotPayout = 50;

        private UISlotMachineView _slotMachineView;
        [SerializeField]
        private SlotMachine _slotMachine;

        private RollingButton _rollingButton;
        private Agent _activeAgent;
        private NetworkId _activeAgentId;
        private int _activeWager;
        private Agent _hiddenVisualRootAgent;

        protected override UIGamblingView ResolveView()
        {
            if (_slotMachineView == null && Context != null && Context.UI != null)
            {
                _slotMachineView = Context.UI.Get<UISlotMachineView>();
            }

            return _slotMachineView;
        }

        protected override void OnViewOpened(UIGamblingView view, Agent agent)
        {
            base.OnViewOpened(view, agent);

            _activeAgent = agent;

            HideLocalAgentVisuals(agent);
        }

        protected override void OnViewClosed(UIGamblingView view)
        {
            base.OnViewClosed(view);

            ShowHiddenAgentVisuals();

            _activeAgent = null;
        }

        private void Awake()
        {
            CacheRollingButton();
        }

        private void OnEnable()
        {
            AttachRollingButtonHandler();
            AttachSlotMachineHandlers();
        }

        private void OnDisable()
        {
            DetachRollingButtonHandler();
            DetachSlotMachineHandlers();
        }

        private void CacheRollingButton()
        {
            if (_slotMachine == null)
                return;

            if (_rollingButton == null)
                _rollingButton = _slotMachine.GetComponentInChildren<RollingButton>(true);
        }

        private void AttachRollingButtonHandler()
        {
            CacheRollingButton();

            if (_rollingButton != null)
                _rollingButton.ExternalPressHandler = HandleRollingButtonPressed;
        }

        private void DetachRollingButtonHandler()
        {
            if (_rollingButton != null && _rollingButton.ExternalPressHandler == HandleRollingButtonPressed)
                _rollingButton.ExternalPressHandler = null;
        }

        private void AttachSlotMachineHandlers()
        {
            if (_slotMachine != null)
            {
                _slotMachine.onRollComplete -= HandleRollComplete;
                _slotMachine.onRollComplete += HandleRollComplete;
            }
        }

        private void DetachSlotMachineHandlers()
        {
            if (_slotMachine != null)
            {
                _slotMachine.onRollComplete -= HandleRollComplete;
            }
        }

        private bool HandleRollingButtonPressed()
        {
            var agent = _activeAgent ?? Context?.ObservedAgent;

            Debug.Log($"[SlotMachineNetworked] RollingButton pressed: button={_rollingButton?.name ?? "<missing>"}, agent={agent?.name ?? "<none>"}, runner={(Runner != null ? Runner.name : "<none>")}, stateAuthority={HasStateAuthority}");

            int wager = _slotMachineView != null ? _slotMachineView.CurrentWager : MinWager;
            return TryRequestRoll(agent, wager);
        }

        private void HideLocalAgentVisuals(Agent agent)
        {
            if (agent == null || agent.HasInputAuthority == false)
                return;

            if (agent.VisualRoot != null)
            {
                ShowHiddenAgentVisuals();

                _hiddenVisualRootAgent = agent;
                agent.VisualRoot.SetActive(false);
            }
        }

        private void ShowHiddenAgentVisuals()
        {
            if (_hiddenVisualRootAgent == null)
                return;

            if (_hiddenVisualRootAgent.VisualRoot != null)
            {
                _hiddenVisualRootAgent.VisualRoot.SetActive(true);
            }

            _hiddenVisualRootAgent = null;
        }

        public bool TryStartRoll(Agent agent, int wager)
        {
            return TryRequestRoll(agent, wager);
        }

        public bool CanRoll()
        {
            return _slotMachine != null && _slotMachine.canRoll();
        }

        private bool TryRequestRoll(Agent agent, int wager)
        {
            if (agent == null)
                return false;

            if (Runner == null)
                return false;

            if (CanRoll() == false)
                return false;

            int clampedWager = ClampWager(wager);

            Inventory inventory = agent.Inventory;
            if (inventory == null)
                return false;

            if (HasStateAuthority == false && inventory.Gold < clampedWager)
                return false;

            if (HasStateAuthority == true)
            {
                return StartRoll(agent, clampedWager);
            }

            RPC_RequestRoll(agent.Object.InputAuthority, agent.Object.Id, clampedWager);
            return true;
        }

        private bool StartRoll(Agent agent, int wager)
        {
            _activeAgentId = default;
            _activeWager = 0;

            if (_slotMachine == null)
                return false;

            if (_slotMachine.canRoll() == false)
                return false;

            Inventory inventory = agent != null ? agent.Inventory : null;
            if (inventory == null)
                return false;

            if (inventory.TrySpendGold(wager) == false)
                return false;

            _activeAgentId = agent.Object != null ? agent.Object.Id : default;
            _activeWager = wager;

            int seed = UnityEngine.Random.Range(int.MinValue + 1, int.MaxValue);

            RPC_StartRoll(seed, _activeAgentId, wager);
            _slotMachineView?.RefreshWagerDisplay();

            return true;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestRoll(PlayerRef playerRef, NetworkId agentId, int wager)
        {
            _ = playerRef;

            if (Runner == null)
                return;

            if (Runner.LocalPlayer != playerRef)
                return;

            Agent agent = null;

            if (Runner.TryFindObject(agentId, out NetworkObject agentObject) == true)
            {
                agent = agentObject.GetComponent<Agent>();
            }

            if (agent == null)
                return;

            StartRoll(agent, wager);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_StartRoll(int randomSeed, NetworkId agentId, int wager)
        {
            if (_slotMachine == null)
                return;

            _activeAgentId = agentId;
            _activeWager = wager;

            var previousState = UnityEngine.Random.state;

            UnityEngine.Random.InitState(randomSeed);

            _slotMachine.Roll();
            _rollingButton?.PlayRollAnimation();

            UnityEngine.Random.state = previousState;
        }

        private void HandleRollComplete(int[] scores, int matchScore)
        {
            _ = scores;

            if (HasStateAuthority == false)
                return;

            if (_activeAgentId == default || _activeWager <= 0)
                return;

            int payout = CalculatePayout(matchScore, _activeWager);
            if (payout <= 0)
            {
                ResetActiveRoll();
                return;
            }

            if (Runner != null && Runner.TryFindObject(_activeAgentId, out NetworkObject agentObject) == true)
            {
                var agent = agentObject.GetComponent<Agent>();
                if (agent != null && agent.Inventory != null)
                {
                    agent.Inventory.AddGold(payout);
                }
            }

            ResetActiveRoll();
            _slotMachineView?.RefreshWagerDisplay();
        }

        private int CalculatePayout(int matchScore, int wager)
        {
            if (matchScore < 2)
                return 0;

            int basePayout = matchScore >= 3 ? _jackpotPayout : _pairMatchPayout;
            float scale = Mathf.Max(1f, wager / (float)MinWager);

            return Mathf.RoundToInt(basePayout * scale);
        }

        private int ClampWager(int wager)
        {
            int clamped = Mathf.Clamp(wager, MinWager, MaxWager);
            int remainder = clamped % WagerStep;
            if (remainder != 0)
            {
                clamped -= remainder;
            }

            return Mathf.Max(MinWager, clamped);
        }

        private void ResetActiveRoll()
        {
            _activeAgentId = default;
            _activeWager = 0;
        }
    }
}
