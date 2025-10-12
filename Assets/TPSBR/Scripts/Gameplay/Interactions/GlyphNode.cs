using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class GlyphNode : NetworkBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField] private string _interactionName = "Harvest Glyph";
        [SerializeField, TextArea] private string _interactionDescription = "Hold interact to harvest this glyph.";
        [SerializeField] private Transform _hudPivot;
        [SerializeField] private Collider _interactionCollider;

        [Header("Harvesting")]
        [SerializeField, Tooltip("Time in seconds the player needs to harvest before the glyph node is depleted.")]
        private float _requiredHarvestTime = 3f;
        [SerializeField, Tooltip("Optional respawn time. Set to zero to keep the node depleted once harvested.")]
        private float _respawnTime;

        [Networked, HideInInspector] private bool IsDepleted { get; set; }
        [Networked, HideInInspector] private TickTimer RespawnTimer { get; set; }

        private Agent _activeHarvester;
        private float _harvestProgress;

        public event Action<Agent> HarvestStarted;
        public event Action<Agent> HarvestCancelled;
        public event Action<Agent> HarvestCompleted;

        string IInteraction.Name => _interactionName;
        string IInteraction.Description => _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => IsDepleted == false && _activeHarvester == null;

        public bool TryBeginHarvest(Agent agent)
        {
            if (agent == null)
                return false;

            if (IsDepleted == true)
                return false;

            if (_activeHarvester != null && _activeHarvester != agent)
                return false;

            _activeHarvester = agent;
            _harvestProgress = 0f;

            RefreshInteractionState();
            HarvestStarted?.Invoke(agent);

            return true;
        }

        public void CancelHarvest(Agent agent)
        {
            if (_activeHarvester != agent)
                return;

            _activeHarvester = null;
            _harvestProgress = 0f;

            RefreshInteractionState();
            HarvestCancelled?.Invoke(agent);
        }

        public bool TickHarvest(float deltaTime, Agent agent)
        {
            if (_activeHarvester != agent)
                return false;

            if (IsDepleted == true)
            {
                _activeHarvester = null;
                RefreshInteractionState();
                return false;
            }

            _harvestProgress += Mathf.Max(0f, deltaTime);

            if (_harvestProgress < _requiredHarvestTime)
                return false;

            CompleteHarvest(agent);
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

        private void CompleteHarvest(Agent agent)
        {
            _activeHarvester = null;
            _harvestProgress = 0f;

            if (HasStateAuthority == true)
            {
                IsDepleted = true;

                if (_respawnTime > 0f)
                {
                    RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnTime);
                }
            }

            RefreshInteractionState();
            HarvestCompleted?.Invoke(agent);
        }

        private void RefreshInteractionState()
        {
            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = IsDepleted == false && _activeHarvester == null;
            }
        }
    }
}
