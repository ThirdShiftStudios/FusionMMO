using Fusion;
using FusionMMO.Dungeons;
using UnityEngine;

namespace TPSBR
{
    public sealed class DungeonExit : ContextBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField] private string _interactionName = "Dungeon Exit";
        [SerializeField, TextArea] private string _interactionDescription = "Leave the dungeon.";
        [SerializeField] private Transform _hudPivot;
        [SerializeField] private Collider _interactionCollider;

        [Header("References")]
        [SerializeField] private NetworkedDungeon _dungeon;

        string IInteraction.Name => _interactionName;
        string IInteraction.Description => _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => isActiveAndEnabled == true && (_interactionCollider == null || (_interactionCollider.enabled == true && _interactionCollider.gameObject.activeInHierarchy == true));

        bool IInteraction.Interact(in InteractionContext context, out string message)
        {
            message = string.Empty;

            Agent agent = context.Agent;
            if (agent == null)
            {
                message = "No agent available.";
                return false;
            }

            NetworkedDungeon dungeon = ResolveDungeon();
            if (dungeon == null)
            {
                message = "Dungeon not available.";
                return false;
            }

            if (dungeon.TryGetEntrance(out var entrance) == false)
            {
                message = "Dungeon entrance unavailable.";
                return false;
            }

            PlayerRef playerRef = agent.Object != null ? agent.Object.InputAuthority : PlayerRef.None;
            if (playerRef == PlayerRef.None)
            {
                message = "Player reference unavailable.";
                return false;
            }

            if (entrance.ExitTransform == null)
            {
                message = "Dungeon exit transform not configured.";
                return false;
            }

            if (agent.Character == null)
            {
                message = "No character available.";
                return false;
            }

            entrance.RequestLoadingScreen(playerRef);

            if (dungeon.TryTeleportPlayerToEntranceExit(agent, out string teleportMessage) == false)
            {
                message = teleportMessage;
                dungeon.RequestLoadingSceneHide(playerRef);
                return false;
            }

            return true;
        }

        private NetworkedDungeon ResolveDungeon()
        {
            if (_dungeon != null)
            {
                return _dungeon;
            }

            _dungeon = GetComponentInParent<NetworkedDungeon>();
            return _dungeon;
        }

        private void Awake()
        {
            if (_interactionCollider == null)
            {
                _interactionCollider = GetComponent<Collider>();
            }
        }

        private void OnValidate()
        {
            if (_dungeon == null)
            {
                _dungeon = GetComponentInParent<NetworkedDungeon>();
            }

            if (_interactionCollider == null)
            {
                _interactionCollider = GetComponent<Collider>();
            }
        }
    }
}
