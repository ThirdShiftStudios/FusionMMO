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
        [Networked, OnChangedRender(nameof(OnActiveAgentObjectChanged)), HideInInspector] private NetworkObjectRef ActiveAgentObject { get; set; }

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

            NetworkObjectRef activeObject = ActiveAgentObject;

            if (activeObject.IsValid == true)
            {
                if (agent.Object != null)
                {
                    NetworkObjectRef agentObjectRef = agent.Object;

                    if (activeObject == agentObjectRef)
                    {
                        if (_activeAgent != agent)
                        {
                            _activeAgent = agent;
                        }

                        return true;
                    }
                }

                if (TryResolveActiveAgent(activeObject, out Agent resolvedAgent) == true)
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
            OnActiveAgentObjectChanged();
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
            _hasSpeculativeAgent = false;
            UpdateActiveAgentRef();
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
            UpdateActiveAgentRef();
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
                UpdateActiveAgentRef();
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
                if (ActiveAgentObject.IsValid == true)
                {
                    _hasSpeculativeAgent = false;

                    if (TryResolveActiveAgent(ActiveAgentObject, out Agent resolvedAgent) == true)
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
            UpdateActiveAgentRef();
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

        private void UpdateActiveAgentRef()
        {
            if (HasStateAuthority == true)
            {
                ActiveAgentObject = _activeAgent != null && _activeAgent.Object != null ? _activeAgent.Object : default;
            }
        }

        private bool HasActiveAgent()
        {
            if (HasStateAuthority == true)
            {
                return _activeAgent != null;
            }

            NetworkObjectRef activeObject = ActiveAgentObject;

            if (activeObject.IsValid == true)
            {
                _hasSpeculativeAgent = false;

                if (TryResolveActiveAgent(activeObject, out Agent resolvedAgent) == true)
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

        private void OnActiveAgentObjectChanged()
        {
            if (HasStateAuthority == true)
            {
                RefreshInteractionState();
                return;
            }

            if (ActiveAgentObject.IsValid == true && TryResolveActiveAgent(ActiveAgentObject, out Agent resolvedAgent) == true)
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

            if (ActiveAgentObject.IsValid == true)
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

        private bool TryResolveActiveAgent(NetworkObjectRef objectRef, out Agent resolvedAgent)
        {
            resolvedAgent = null;

            if (Runner == null || objectRef.IsValid == false)
                return false;

            if (Runner.TryFindObject(objectRef, out NetworkObject networkObject) == false || networkObject == null)
                return false;

            resolvedAgent = networkObject.GetComponent<Agent>();
            return resolvedAgent != null;
        }
    }
}
