using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

namespace TPSBR
{
    public abstract class ResourceNode : NetworkBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField] private string _interactionName = "Harvest Resource";
        [SerializeField, TextArea] private string _interactionDescription = "Hold interact to harvest this resource.";
        [SerializeField] private Transform _hudPivot;
        [SerializeField] private Collider _interactionCollider;

        [Header("Resource")]
        [FormerlySerializedAs("_requiredChopTime")]
        [FormerlySerializedAs("_requiredMiningTime")]
        [SerializeField, Tooltip("Time in seconds the player needs to interact before the resource is depleted.")]
        private float _requiredInteractionTime = 3f;
        [SerializeField, Tooltip("Optional respawn time. Set to zero to keep the resource depleted once harvested.")]
        private float _respawnTime;
        [SerializeField, Tooltip("Additional interaction speed gained per tool Speed point.")]
        private float _toolSpeedMultiplier = 0.05f;

        [Networked, HideInInspector] private bool IsDepleted { get; set; }
        [Networked, HideInInspector] private TickTimer RespawnTimer { get; set; }
        [Networked, HideInInspector] private float InteractionProgress { get; set; }

        private Agent _activeAgent;

        public float InteractionProgressNormalized => _requiredInteractionTime > 0f ? Mathf.Clamp01(InteractionProgress / _requiredInteractionTime) : 0f;

        public bool IsInteracting(Agent agent)
        {
            return _activeAgent != null && _activeAgent == agent;
        }

        string IInteraction.Name => string.IsNullOrWhiteSpace(_interactionName) ? GetDefaultInteractionName() : _interactionName;
        string IInteraction.Description => string.IsNullOrWhiteSpace(_interactionDescription) ? GetDefaultInteractionDescription() : _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => IsDepleted == false && _activeAgent == null;

        protected virtual void Reset()
        {
            _interactionName = GetDefaultInteractionName();
            _interactionDescription = GetDefaultInteractionDescription();
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

        protected bool TryBeginInteraction(Agent agent)
        {
            if (agent == null)
                return false;

            if (IsDepleted == true)
                return false;

            if (_activeAgent != null && _activeAgent != agent)
                return false;

            _activeAgent = agent;
            InteractionProgress = 0f;

            RefreshInteractionState();
            OnInteractionStarted(agent);

            return true;
        }

        protected void CancelInteraction(Agent agent)
        {
            if (_activeAgent != agent)
                return;

            _activeAgent = null;
            InteractionProgress = 0f;

            RefreshInteractionState();
            OnInteractionCancelled(agent);
        }

        protected bool TickInteraction(float deltaTime, Agent agent)
        {
            if (HasStateAuthority == false)
                return false;

            if (_activeAgent != agent)
                return false;

            if (IsDepleted == true)
            {
                _activeAgent = null;
                RefreshInteractionState();
                return false;
            }

            float adjustedDelta = CalculateProgressDelta(deltaTime, agent);
            InteractionProgress += adjustedDelta;

            if (InteractionProgress < _requiredInteractionTime)
                return false;

            CompleteInteraction(agent);
            return true;
        }

        protected void ResetResource()
        {
            if (HasStateAuthority == false)
                return;

            IsDepleted = false;
            RespawnTimer = default;
            InteractionProgress = 0f;
            RefreshInteractionState();
        }

        protected virtual float CalculateProgressDelta(float deltaTime, Agent agent)
        {
            float progress = Mathf.Max(0f, deltaTime);

            if (agent == null)
                return progress;

            int toolSpeed = Mathf.Max(0, GetToolSpeed(agent));
            if (toolSpeed <= 0)
                return progress;

            return progress * (1f + toolSpeed * _toolSpeedMultiplier);
        }

        protected abstract string GetDefaultInteractionName();
        protected abstract string GetDefaultInteractionDescription();
        protected abstract int GetToolSpeed(Agent agent);

        protected virtual void OnInteractionStarted(Agent agent)
        {
        }

        protected virtual void OnInteractionCancelled(Agent agent)
        {
        }

        protected virtual void OnInteractionCompleted(Agent agent)
        {
        }

        protected virtual void RefreshInteractionState()
        {
            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = IsDepleted == false && _activeAgent == null;
            }
        }

        private void CompleteInteraction(Agent agent)
        {
            _activeAgent = null;
            InteractionProgress = 0f;

            if (HasStateAuthority == true)
            {
                IsDepleted = true;

                if (_respawnTime > 0f)
                {
                    RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnTime);
                }
            }

            RefreshInteractionState();
            OnInteractionCompleted(agent);
        }
    }
}
