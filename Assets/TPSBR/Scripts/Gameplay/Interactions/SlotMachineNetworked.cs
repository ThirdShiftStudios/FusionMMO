using Fusion;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public sealed class SlotMachineNetworked : GamblingMachine
    {
        private UISlotMachineView _slotMachineView;
        [SerializeField]
        private SlotMachine _slotMachine;

        private RollingButton _rollingButton;
        private Agent _activeAgent;

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
        }

        protected override void OnViewClosed(UIGamblingView view)
        {
            base.OnViewClosed(view);

            _activeAgent = null;
        }

        private void Awake()
        {
            CacheRollingButton();
        }

        private void OnEnable()
        {
            AttachRollingButtonHandler();
        }

        private void OnDisable()
        {
            DetachRollingButtonHandler();
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
            if (_activeAgent == null || Runner == null)
                return true;

            if (HasStateAuthority == true)
            {
                StartRoll();
            }
            else
            {
                RPC_RequestRoll(_activeAgent.Object.InputAuthority, _activeAgent.Object.Id);
            }

            return true;
        }

        private void StartRoll()
        {
            if (_slotMachine == null)
                return;

            if (_slotMachine.canRoll() == false)
                return;

            int seed = UnityEngine.Random.Range(int.MinValue + 1, int.MaxValue);

            RPC_StartRoll(seed);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestRoll(PlayerRef playerRef, NetworkId agentId)
        {
            if (Runner == null || Runner.LocalPlayer != playerRef)
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

            StartRoll();
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
    }
}
