using Fusion;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public sealed class SlotMachineNetworked : GamblingMachine
    {
        public const int MinBetAmount = 5;
        public const int MaxBetAmount = 100;
        public const int BetStep = 5;

        private UISlotMachineView _slotMachineView;
        [SerializeField]
        private SlotMachine _slotMachine;

        private RollingButton _rollingButton;
        private Agent _activeAgent;
        private Agent _payoutAgent;
        private int _currentBet = MinBetAmount;
        private int _betInPlay;

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
            SetBetAmount(_currentBet);
        }

        protected override void OnViewClosed(UIGamblingView view)
        {
            base.OnViewClosed(view);

            _activeAgent = null;
            _payoutAgent = null;
            _betInPlay = 0;
        }

        private void Awake()
        {
            CacheRollingButton();
            SubscribeSlotMachineCallbacks();
        }

        private void OnEnable()
        {
            AttachRollingButtonHandler();
            SubscribeSlotMachineCallbacks();
        }

        private void OnDisable()
        {
            DetachRollingButtonHandler();
            UnsubscribeSlotMachineCallbacks();
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

        private bool HandleRollingButtonPressed()
        {
            return TryPlaceBetAndRoll();
        }

        public void SetBetAmount(int amount)
        {
            _currentBet = Mathf.Clamp(amount, MinBetAmount, MaxBetAmount);
        }

        public bool TryPlaceBetAndRoll()
        {
            var agent = _activeAgent ?? Context?.ObservedAgent;

            Debug.Log($"[SlotMachineNetworked] RollingButton pressed: button={_rollingButton?.name ?? "<missing>"}, agent={agent?.name ?? "<none>"}, runner={(Runner != null ? Runner.name : "<none>")}, stateAuthority={HasStateAuthority}, bet={_currentBet}");

            if (agent == null)
                return false;

            if (agent.Inventory == null)
                return false;

            int affordableBet = Mathf.Min(agent.Inventory.Gold, MaxBetAmount);

            if (affordableBet < MinBetAmount)
                return false;

            if (_currentBet > affordableBet)
            {
                SetBetAmount(affordableBet);
            }

            if (Runner == null)
                return false;

            if (HasStateAuthority == true)
            {
                return StartRoll(agent, _currentBet);
            }
            else
            {
                RPC_RequestRoll(agent.Object.InputAuthority, agent.Object.Id, _currentBet);
            }

            return true;
        }

        private bool StartRoll(Agent agent, int betAmount)
        {
            _payoutAgent = null;
            _betInPlay = 0;

            if (_slotMachine == null)
                return false;

            if (_slotMachine.canRoll() == false)
                return false;

            if (agent == null)
                return false;

            if (agent.Inventory == null)
                return false;

            betAmount = Mathf.Clamp(betAmount, MinBetAmount, MaxBetAmount);

            if (agent.Inventory.TrySpendGold(betAmount) == false)
                return false;

            _betInPlay = betAmount;
            _payoutAgent = agent;

            int seed = UnityEngine.Random.Range(int.MinValue + 1, int.MaxValue);

            RPC_StartRoll(seed);

            return true;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestRoll(PlayerRef playerRef, NetworkId agentId, int betAmount)
        {
            if (Runner == null)
                return;

            if (Runner.LocalPlayer != playerRef)
                return;

            Agent agent = null;

            if (Runner.TryFindObject(agentId, out NetworkObject agentObject) == true)
            {
                agent = agentObject.GetComponent<Agent>();
            }

            if (agent == null && Context != null)
            {
                agent = Context.ObservedAgent;
            }

            if (agent == null)
                return;

            StartRoll(agent, betAmount);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_StartRoll(int randomSeed)
        {
            if (_slotMachine == null)
                return;

            var previousState = UnityEngine.Random.state;

            UnityEngine.Random.InitState(randomSeed);

            _slotMachine.Roll();
            _rollingButton?.PlayRollAnimation();

            UnityEngine.Random.state = previousState;
        }

        private void SubscribeSlotMachineCallbacks()
        {
            if (_slotMachine != null)
            {
                _slotMachine.onRollComplete -= HandleRollComplete;
                _slotMachine.onRollComplete += HandleRollComplete;
            }
        }

        private void UnsubscribeSlotMachineCallbacks()
        {
            if (_slotMachine != null)
            {
                _slotMachine.onRollComplete -= HandleRollComplete;
            }
        }

        private void HandleRollComplete(int[] score, int matchScore)
        {
            _ = score;

            if (HasStateAuthority == false)
                return;

            if (_betInPlay <= 0)
                return;

            if (_payoutAgent == null || _payoutAgent.Inventory == null)
            {
                _betInPlay = 0;
                return;
            }

            if (matchScore > 1)
            {
                int payout = _betInPlay * matchScore;
                _payoutAgent.Inventory.AddGold(payout);
            }

            _betInPlay = 0;
            _payoutAgent = null;
        }
    }
}
