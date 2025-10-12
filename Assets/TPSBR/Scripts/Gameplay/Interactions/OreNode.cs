using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class OreNode : NetworkBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField] private string _interactionName = "Mine Ore";
        [SerializeField, TextArea] private string _interactionDescription = "Hold interact to mine this ore node.";
        [SerializeField] private Transform _hudPivot;
        [SerializeField] private Collider _interactionCollider;

        [Header("Mining")]
        [SerializeField, Tooltip("Time in seconds the player needs to mine before the node is depleted.")]
        private float _requiredMiningTime = 3f;
        [SerializeField, Tooltip("Optional respawn time. Set to zero to keep the node depleted once mined.")]
        private float _respawnTime;

        [Networked, HideInInspector] private bool IsDepleted { get; set; }
        [Networked, HideInInspector] private TickTimer RespawnTimer { get; set; }

        private Agent _activeMiner;
        private float _miningProgress;

        public event Action<Agent> MiningStarted;
        public event Action<Agent> MiningCancelled;
        public event Action<Agent> MiningCompleted;

        string IInteraction.Name => _interactionName;
        string IInteraction.Description => _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => IsDepleted == false && _activeMiner == null;

        public bool TryBeginMining(Agent agent)
        {
            if (agent == null)
                return false;

            if (IsDepleted == true)
                return false;

            if (_activeMiner != null && _activeMiner != agent)
                return false;

            _activeMiner = agent;
            _miningProgress = 0f;

            RefreshInteractionState();
            MiningStarted?.Invoke(agent);

            return true;
        }

        public void CancelMining(Agent agent)
        {
            if (_activeMiner != agent)
                return;

            _activeMiner = null;
            _miningProgress = 0f;

            RefreshInteractionState();
            MiningCancelled?.Invoke(agent);
        }

        public bool TickMining(float deltaTime, Agent agent)
        {
            if (_activeMiner != agent)
                return false;

            if (IsDepleted == true)
            {
                _activeMiner = null;
                RefreshInteractionState();
                return false;
            }

            _miningProgress += Mathf.Max(0f, deltaTime);

            if (_miningProgress < _requiredMiningTime)
                return false;

            CompleteMining(agent);
            return true;
        }

        public void ResetNode()
        {
            if (HasStateAuthority == false)
                return;

            IsDepleted = false;
            RespawnTimer = default;
            RefreshInteractionState();
        }

        public override void Spawned()
        {
            base.Spawned();

            RefreshInteractionState();
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
                return;

            if (IsDepleted == false)
                return;

            if (_respawnTime <= 0f)
                return;

            if (RespawnTimer.IsRunning == false)
                return;

            if (RespawnTimer.Expired(Runner) == false)
                return;

            IsDepleted = false;
            RespawnTimer = default;
            RefreshInteractionState();
        }

        public override void Render()
        {
            RefreshInteractionState();
        }

        private void CompleteMining(Agent agent)
        {
            _activeMiner = null;
            _miningProgress = 0f;

            if (HasStateAuthority == true)
            {
                IsDepleted = true;

                if (_respawnTime > 0f)
                {
                    RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnTime);
                }
            }

            RefreshInteractionState();
            MiningCompleted?.Invoke(agent);
        }

        private void RefreshInteractionState()
        {
            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = IsDepleted == false && _activeMiner == null;
            }
        }
    }
}
