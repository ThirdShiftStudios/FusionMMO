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
        [Networked, HideInInspector] private float SyncedInteractionProgress { get; set; }
        [Networked, OnChangedRender(nameof(OnActiveAgentIdChanged)), HideInInspector] private NetworkBehaviourId ActiveAgentId { get; set; }

        private Agent _activeAgent;
        private bool _hasSpeculativeAgent;
        private float _interactionProgress;

        public float InteractionProgressNormalized
        {
            get
            {
                float progress = HasStateAuthority == true ? _interactionProgress : SyncedInteractionProgress;
                return _requiredInteractionTime > 0f ? Mathf.Clamp01(progress / _requiredInteractionTime) : 0f;
            }
        }

        public bool IsInteracting(Agent agent)
        {
            if (agent == null)
                return false;

            if (agent.Object == null)
                return false;

            if (HasStateAuthority == true)
            {
                return _activeAgent != null && _activeAgent == agent;
            }

            NetworkBehaviourId activeId = ActiveAgentId;

            if (activeId.IsValid == true)
            {
                NetworkBehaviourId agentId = agent;

                if (agentId.IsValid == true && activeId == agentId)
                {
                    if (_activeAgent != agent)
                    {
                        _activeAgent = agent;
                    }

                    return true;
                }

                if (TryResolveActiveAgent(activeId, out Agent resolvedAgent) == true)
                {
                    if (_activeAgent != resolvedAgent)
                    {
                        _activeAgent = resolvedAgent;
                    }

                    return resolvedAgent == agent;
                }

                return false;
            }

            return _activeAgent != null && _activeAgent == agent;
        }

        public bool ShouldReleaseLocalAgent(Agent agent)
        {
            if (agent == null)
                return true;

            if (HasStateAuthority == true)
                return IsInteracting(agent) == false;

            NetworkBehaviourId activeId = ActiveAgentId;

            if (activeId.IsValid == true)
            {
                if (TryResolveActiveAgent(activeId, out Agent resolvedAgent) == true)
                {
                    if (_activeAgent != resolvedAgent)
                    {
                        _activeAgent = resolvedAgent;
                    }

                    return resolvedAgent != agent;
                }

                // Keep the speculative latch until the authoritative agent can be resolved.
                return false;
            }

            if (_hasSpeculativeAgent == true && _activeAgent == agent)
                return false;

            return true;
        }

        string IInteraction.Name => string.IsNullOrWhiteSpace(_interactionName) ? GetDefaultInteractionName() : _interactionName;
        string IInteraction.Description => string.IsNullOrWhiteSpace(_interactionDescription) ? GetDefaultInteractionDescription() : _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => IsDepleted == false && HasActiveAgent() == false;

        protected virtual void Reset()
        {
            _interactionName = GetDefaultInteractionName();
            _interactionDescription = GetDefaultInteractionDescription();
        }

        public override void Spawned()
        {
            base.Spawned();
            RefreshInteractionState();
            OnActiveAgentIdChanged();
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
            _hasSpeculativeAgent = HasStateAuthority == false;
            UpdateActiveAgentId();
            _interactionProgress = 0f;
            UpdateSyncedInteractionProgress();

            RefreshInteractionState();
            OnInteractionStarted(agent);

            return true;
        }

        protected void CancelInteraction(Agent agent)
        {
            if (_activeAgent != agent)
                return;

            _activeAgent = null;
            _hasSpeculativeAgent = false;
            UpdateActiveAgentId();
            _interactionProgress = 0f;
            UpdateSyncedInteractionProgress();

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
                _hasSpeculativeAgent = false;
                UpdateActiveAgentId();
                RefreshInteractionState();
                return false;
            }

            float adjustedDelta = CalculateProgressDelta(deltaTime, agent);
            _interactionProgress += adjustedDelta;
            UpdateSyncedInteractionProgress();

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
                if (ActiveAgentId.IsValid == true)
                {
                    _hasSpeculativeAgent = false;

                    if (TryResolveActiveAgent(ActiveAgentId, out Agent resolvedAgent) == true)
                    {
                        if (_activeAgent != resolvedAgent)
                        {
                            _activeAgent = resolvedAgent;
                        }
                    }
                    else if (_activeAgent != null)
                    {
                        _activeAgent = null;
                    }
                }
                else if (_hasSpeculativeAgent == false && _activeAgent != null)
                {
                    _activeAgent = null;
                }
            }

            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = IsDepleted == false && HasActiveAgent() == false;
            }
        }

        private void CompleteInteraction(Agent agent)
        {
            _activeAgent = null;
            _hasSpeculativeAgent = false;
            UpdateActiveAgentId();
            _interactionProgress = 0f;
            UpdateSyncedInteractionProgress();

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

        private void UpdateSyncedInteractionProgress()
        {
            if (HasStateAuthority == true)
            {
                SyncedInteractionProgress = _interactionProgress;
            }
        }

        private void UpdateActiveAgentId()
        {
            if (HasStateAuthority == true)
            {
                ActiveAgentId = _activeAgent != null ? (NetworkBehaviourId)_activeAgent : NetworkBehaviourId.None;
            }
        }

        private bool HasActiveAgent()
        {
            if (HasStateAuthority == true)
            {
                return _activeAgent != null;
            }

            NetworkBehaviourId activeId = ActiveAgentId;

            if (activeId.IsValid == true)
            {
                _hasSpeculativeAgent = false;

                if (TryResolveActiveAgent(activeId, out Agent resolvedAgent) == true)
                {
                    if (_activeAgent != resolvedAgent)
                    {
                        _activeAgent = resolvedAgent;
                    }
                }
                else if (_activeAgent != null)
                {
                    _activeAgent = null;
                }

                return true;
            }

            if (_activeAgent != null)
            {
                return true;
            }

            return false;
        }

        private void OnActiveAgentIdChanged()
        {
            if (HasStateAuthority == true)
            {
                RefreshInteractionState();
                return;
            }

            if (ActiveAgentId.IsValid == true && TryResolveActiveAgent(ActiveAgentId, out Agent resolvedAgent) == true)
            {
                _activeAgent = resolvedAgent;
                _hasSpeculativeAgent = false;
            }
            else
            {
                _activeAgent = null;
                _hasSpeculativeAgent = false;
            }

            RefreshInteractionState();
        }

        public void LatchLocalAgent(Agent agent)
        {
            if (HasStateAuthority == true)
                return;

            if (ActiveAgentId.IsValid == true)
                return;

            if (agent == null || agent.Object == null)
            {
                if (_hasSpeculativeAgent == true)
                {
                    _hasSpeculativeAgent = false;
                    _activeAgent = null;
                    RefreshInteractionState();
                }

                return;
            }

            _activeAgent = agent;
            _hasSpeculativeAgent = true;
            RefreshInteractionState();
        }

        private bool TryResolveActiveAgent(NetworkBehaviourId behaviourId, out Agent resolvedAgent)
        {
            resolvedAgent = null;

            if (Runner == null || behaviourId.IsValid == false)
                return false;

            if (Runner.TryFindBehaviour(behaviourId, out Agent agent) == false || agent == null)
                return false;

            resolvedAgent = agent;
            return true;
        }
    }
}
