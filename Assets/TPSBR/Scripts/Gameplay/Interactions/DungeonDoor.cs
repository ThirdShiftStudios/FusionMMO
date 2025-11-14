using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class DungeonDoor : StaticNetworkTransform, IInteraction
    {
        public enum EDungeonDoorState
        {
            None,
            Initializing,
            Open,
            Closed,
        }

        [Header("Interaction")]
        [SerializeField] private string _interactionName = "Dungeon Door";
        [SerializeField, TextArea] private string _interactionDescription = "Open or close the door.";
        [SerializeField] private Transform _hudPivot;
        [SerializeField] private Collider _interactionCollider;

        [Header("Animation")]
        [SerializeField] private Animation _animation;
        [SerializeField] private AnimationClip _openAnimation;
        [SerializeField] private AnimationClip _closeAnimation;

        [Header("Settings")]
        [SerializeField] private EDungeonDoorState _startState = EDungeonDoorState.Closed;
        [SerializeField] private float _interactionCooldown = 1f;

        [Networked, HideInInspector] public EDungeonDoorState DoorState { get; private set; }
        [Networked, HideInInspector] private TickTimer InteractionCooldown { get; set; }

        private EDungeonDoorState _localState = EDungeonDoorState.None;

        string IInteraction.Name => _interactionName;
        string IInteraction.Description => _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => DoorState != EDungeonDoorState.None && DoorState != EDungeonDoorState.Initializing;

        bool IInteraction.Interact(in InteractionContext context, out string message)
        {
            message = string.Empty;

            if (HasStateAuthority == false)
            {
                return false;
            }

            if (DoorState == EDungeonDoorState.None || DoorState == EDungeonDoorState.Initializing)
            {
                return false;
            }

            if (InteractionCooldown.ExpiredOrNotRunning(Runner) == false)
            {
                return false;
            }

            ToggleState();
            return true;
        }

        private void Awake()
        {
            if (_animation == null)
            {
                _animation = GetComponent<Animation>();
            }
        }

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority == true)
            {
                DoorState = GetValidStartState();
                InteractionCooldown = default;
            }

            UpdateLocalState();
        }

        public override void Render()
        {
            base.Render();
            UpdateLocalState();
        }

        private void ToggleState()
        {
            switch (DoorState)
            {
                case EDungeonDoorState.Open:
                    SetState(EDungeonDoorState.Closed);
                    break;
                case EDungeonDoorState.Closed:
                    SetState(EDungeonDoorState.Open);
                    break;
            }
        }

        private void SetState(EDungeonDoorState newState)
        {
            DoorState = newState;
            InteractionCooldown = TickTimer.CreateFromSeconds(Runner, _interactionCooldown);
            UpdateLocalState();
        }

        private EDungeonDoorState GetValidStartState()
        {
            switch (_startState)
            {
                case EDungeonDoorState.Open:
                case EDungeonDoorState.Closed:
                    return _startState;
                default:
                    return EDungeonDoorState.Closed;
            }
        }

        private void UpdateLocalState()
        {
            if (_localState == DoorState)
            {
                return;
            }

            _localState = DoorState;

            if (_interactionCollider != null)
            {
                bool enableCollider = _localState != EDungeonDoorState.None && _localState != EDungeonDoorState.Initializing;
                _interactionCollider.enabled = enableCollider;
            }

            if (_animation == null)
            {
                return;
            }

            switch (_localState)
            {
                case EDungeonDoorState.Open:
                    PlayAnimation(_openAnimation);
                    break;
                case EDungeonDoorState.Closed:
                    PlayAnimation(_closeAnimation);
                    break;
            }
        }

        private void PlayAnimation(AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (_animation.clip != clip)
            {
                _animation.clip = clip;
            }

            _animation.Play();
        }
    }
}
