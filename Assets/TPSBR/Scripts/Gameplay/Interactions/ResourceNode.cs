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

        [Header("Lifecycle")]
        [SerializeField, Tooltip("Delay before the resource node despawns after being depleted.")]
        private float _despawnDelay = 2f;

        [Networked, HideInInspector] private bool IsDepleted { get; set; }
        [Networked, HideInInspector] private TickTimer RespawnTimer { get; set; }
        [Networked, HideInInspector] private TickTimer DespawnTimer { get; set; }
        [Networked, HideInInspector] private float InteractionProgress { get; set; }
        [Networked, HideInInspector] private float InteractionProgressRate { get; set; }
        [Networked, HideInInspector] private float LastProgressUpdateTime { get; set; }
        [Networked, HideInInspector] private PlayerRef ActiveInteractor { get; set; }

        private Agent _activeAgent;

        public float InteractionProgressNormalized
        {
            get
            {
                if (_requiredInteractionTime <= 0f)
                    return 0f;

                float predictedProgress = GetPredictedInteractionProgress();
                return Mathf.Clamp01(predictedProgress / _requiredInteractionTime);
            }
        }

        public bool IsInteracting(Agent agent)
        {
            if (agent == null)
                return false;

            if (_activeAgent != null)
                return _activeAgent == agent;

            if (ActiveInteractor == PlayerRef.None)
                return false;

            NetworkObject agentObject = agent.Object;
            if (agentObject == null)
                return false;

            return agentObject.InputAuthority == ActiveInteractor;
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
            DespawnTimer = default;
            RefreshInteractionState();
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
                return;

            if (DespawnTimer.IsRunning == true)
            {
                if (DespawnTimer.Expired(Runner) == false)
                    return;

                if (Object != null && Object.IsValid == true)
                {
                    Runner.Despawn(Object);
                }

                DespawnTimer = default;
                return;
            }

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
            InteractionProgressRate = 0f;
            ActiveInteractor = GetPlayerRef(agent);
            LastProgressUpdateTime = GetCurrentSimulationTime();

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
            InteractionProgressRate = 0f;
            ActiveInteractor = PlayerRef.None;
            LastProgressUpdateTime = GetCurrentSimulationTime();

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
            InteractionProgressRate = deltaTime > 0f ? adjustedDelta / deltaTime : 0f;
            LastProgressUpdateTime = GetCurrentSimulationTime();

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
            InteractionProgressRate = 0f;
            ActiveInteractor = PlayerRef.None;
            LastProgressUpdateTime = GetCurrentSimulationTime();
            DespawnTimer = default;
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
                bool hasInteractor = _activeAgent != null || ActiveInteractor != PlayerRef.None;
                _interactionCollider.enabled = IsDepleted == false && hasInteractor == false;
            }
        }

        private void CompleteInteraction(Agent agent)
        {
            _activeAgent = null;
            InteractionProgress = 0f;
            InteractionProgressRate = 0f;
            ActiveInteractor = PlayerRef.None;
            LastProgressUpdateTime = GetCurrentSimulationTime();

            if (HasStateAuthority == true)
            {
                IsDepleted = true;

                if (_respawnTime > 0f)
                {
                    RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnTime);
                }

                if (_despawnDelay > 0f)
                {
                    DespawnTimer = TickTimer.CreateFromSeconds(Runner, _despawnDelay);
                }
            }

            RefreshInteractionState();
            OnInteractionCompleted(agent);
        }

        private float GetPredictedInteractionProgress()
        {
            float progress = InteractionProgress;

            if (InteractionProgressRate > 0f)
            {
                float currentTime = GetCurrentSimulationTime();
                float deltaTime = Mathf.Max(0f, currentTime - LastProgressUpdateTime);
                if (deltaTime > 0f)
                {
                    progress += InteractionProgressRate * deltaTime;
                }
            }

            if (_requiredInteractionTime > 0f)
            {
                progress = Mathf.Min(progress, _requiredInteractionTime);
            }

            return progress;
        }

        private float GetCurrentSimulationTime()
        {
            if (Runner != null && Runner.IsRunning == true)
            {
                return (float)Runner.SimulationTime;
            }

            return Time.time;
        }

        private PlayerRef GetPlayerRef(Agent agent)
        {
            if (agent == null)
                return PlayerRef.None;

            NetworkObject agentObject = agent.Object;
            if (agentObject == null)
                return PlayerRef.None;

            return agentObject.InputAuthority;
        }
    }
}
