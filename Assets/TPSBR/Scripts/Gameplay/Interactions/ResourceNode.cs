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
        [Networked, HideInInspector] private float InteractionProgressState { get; set; }
        [Networked, HideInInspector] private PlayerRef ActivePlayerRef { get; set; }

        private Agent _activeAgent;
        private float _interactionProgress;

        private bool IsLocalActiveAgent
        {
            get
            {
                if (_activeAgent == null)
                    return false;

                NetworkObject networkObject = _activeAgent.Object;
                if (networkObject == null)
                    return false;

                return networkObject.HasInputAuthority;
            }
        }

        private bool HasActiveAgent => _activeAgent != null || ActivePlayerRef != PlayerRef.None;

        private float CurrentInteractionProgress
        {
            get
            {
                if (HasStateAuthority == true)
                    return _interactionProgress;

                if (IsLocalActiveAgent == true)
                    return Mathf.Max(_interactionProgress, InteractionProgressState);

                return InteractionProgressState;
            }
        }

        public float InteractionProgressNormalized => _requiredInteractionTime > 0f ? Mathf.Clamp01(CurrentInteractionProgress / _requiredInteractionTime) : 0f;

        public bool IsInteracting(Agent agent)
        {
            if (agent == null)
                return false;

            if (_activeAgent != null)
                return _activeAgent == agent;

            if (ActivePlayerRef == PlayerRef.None)
                return false;

            NetworkObject networkObject = agent.Object;
            if (networkObject == null)
                return false;

            return networkObject.InputAuthority == ActivePlayerRef;
        }

        string IInteraction.Name => string.IsNullOrWhiteSpace(_interactionName) ? GetDefaultInteractionName() : _interactionName;
        string IInteraction.Description => string.IsNullOrWhiteSpace(_interactionDescription) ? GetDefaultInteractionDescription() : _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => IsDepleted == false && HasActiveAgent == false;

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

            SetActiveAgent(agent);
            _interactionProgress = 0f;
            UpdateNetworkedProgress();

            RefreshInteractionState();
            OnInteractionStarted(agent);

            return true;
        }

        protected void CancelInteraction(Agent agent)
        {
            if (_activeAgent != agent)
                return;

            SetActiveAgent(null);
            _interactionProgress = 0f;
            UpdateNetworkedProgress();

            RefreshInteractionState();
            OnInteractionCancelled(agent);
        }

        protected bool TickInteraction(float deltaTime, Agent agent)
        {
            if (_activeAgent != agent)
                return false;

            if (IsDepleted == true)
            {
                _activeAgent = null;
                RefreshInteractionState();
                return false;
            }

            float adjustedDelta = CalculateProgressDelta(deltaTime, agent);
            _interactionProgress += adjustedDelta;
            UpdateNetworkedProgress();

            if (_interactionProgress < _requiredInteractionTime)
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
            UpdateNetworkedProgress();
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
            if (HasStateAuthority == false)
            {
                if (IsLocalActiveAgent == true)
                {
                    if (_interactionProgress < InteractionProgressState)
                    {
                        _interactionProgress = InteractionProgressState;
                    }
                }
                else
                {
                    _interactionProgress = InteractionProgressState;
                }
            }

            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = IsDepleted == false && HasActiveAgent == false;
            }
        }

        private void CompleteInteraction(Agent agent)
        {
            SetActiveAgent(null);
            _interactionProgress = 0f;
            UpdateNetworkedProgress();

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

        private void SetActiveAgent(Agent agent)
        {
            _activeAgent = agent;

            if (HasStateAuthority == true)
            {
                ActivePlayerRef = agent != null && agent.Object != null ? agent.Object.InputAuthority : PlayerRef.None;
            }
        }

        private void UpdateNetworkedProgress()
        {
            if (HasStateAuthority == true)
            {
                InteractionProgressState = _interactionProgress;
            }
        }
    }
}
