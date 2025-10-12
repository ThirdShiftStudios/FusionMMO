using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class TreeNode : NetworkBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField] private string _interactionName = "Chop Tree";
        [SerializeField, TextArea] private string _interactionDescription = "Hold interact to chop down this tree.";
        [SerializeField] private Transform _hudPivot;
        [SerializeField] private Collider _interactionCollider;

        [Header("Chopping")]
        [SerializeField, Tooltip("Time in seconds the player needs to chop before the tree is felled.")]
        private float _requiredChopTime = 3f;
        [SerializeField, Tooltip("Optional respawn time. Set to zero to keep the tree felled once chopped.")]
        private float _respawnTime;

        [Networked, HideInInspector] private bool IsFelled { get; set; }
        [Networked, HideInInspector] private TickTimer RespawnTimer { get; set; }

        private Agent _activeChopper;
        private float _chopProgress;

        public event Action<Agent> ChoppingStarted;
        public event Action<Agent> ChoppingCancelled;
        public event Action<Agent> ChoppingCompleted;

        string IInteraction.Name => _interactionName;
        string IInteraction.Description => _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => IsFelled == false && _activeChopper == null;

        public bool TryBeginChopping(Agent agent)
        {
            if (agent == null)
                return false;

            if (IsFelled == true)
                return false;

            if (_activeChopper != null && _activeChopper != agent)
                return false;

            _activeChopper = agent;
            _chopProgress = 0f;

            RefreshInteractionState();
            ChoppingStarted?.Invoke(agent);

            return true;
        }

        public void CancelChopping(Agent agent)
        {
            if (_activeChopper != agent)
                return;

            _activeChopper = null;
            _chopProgress = 0f;

            RefreshInteractionState();
            ChoppingCancelled?.Invoke(agent);
        }

        public bool TickChopping(float deltaTime, Agent agent)
        {
            if (_activeChopper != agent)
                return false;

            if (IsFelled == true)
            {
                _activeChopper = null;
                RefreshInteractionState();
                return false;
            }

            _chopProgress += Mathf.Max(0f, deltaTime);

            if (_chopProgress < _requiredChopTime)
                return false;

            CompleteChopping(agent);
            return true;
        }

        public void ResetTree()
        {
            if (HasStateAuthority == false)
                return;

            IsFelled = false;
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

            if (IsFelled == false)
                return;

            if (_respawnTime <= 0f)
                return;

            if (RespawnTimer.IsRunning == false)
                return;

            if (RespawnTimer.Expired(Runner) == false)
                return;

            IsFelled = false;
            RespawnTimer = default;
            RefreshInteractionState();
        }

        public override void Render()
        {
            RefreshInteractionState();
        }

        private void CompleteChopping(Agent agent)
        {
            _activeChopper = null;
            _chopProgress = 0f;

            if (HasStateAuthority == true)
            {
                IsFelled = true;

                if (_respawnTime > 0f)
                {
                    RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnTime);
                }
            }

            RefreshInteractionState();
            ChoppingCompleted?.Invoke(agent);
        }

        private void RefreshInteractionState()
        {
            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = IsFelled == false && _activeChopper == null;
            }
        }
    }
}
