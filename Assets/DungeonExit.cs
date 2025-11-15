using Fusion;
using UnityEngine;
using FusionMMO.Dungeons;

namespace TPSBR
{
    public sealed class DungeonExit : ContextBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField] private string _interactionName = "Leave Dungeon";
        [SerializeField, TextArea] private string _interactionDescription = "Return to the entrance.";
        [SerializeField] private Transform _hudPivot;
        [SerializeField] private Collider _interactionCollider;

        [Header("References")]
        [SerializeField] private NetworkedDungeon _networkedDungeon;

        [Networked(OnChanged = nameof(OnNetworkedDungeonChanged))]
        private NetworkBehaviourRef<NetworkedDungeon> _networkedDungeonRef { get; set; }

        string IInteraction.Name => _interactionName;
        string IInteraction.Description => _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => isActiveAndEnabled == true && (_interactionCollider == null || (_interactionCollider.enabled == true && _interactionCollider.gameObject.activeInHierarchy == true));

        bool IInteraction.Interact(in InteractionContext context, out string message)
        {
            message = string.Empty;

            if (HasStateAuthority == false)
            {
                return false;
            }

            if (CacheNetworkedDungeon() == false)
            {
                message = "No dungeon found.";
                return false;
            }

            var entrance = _networkedDungeon.Entrance;
            if (entrance == null)
            {
                message = "No entrance linked.";
                return false;
            }

            var exitTransform = entrance.ExitTransform;
            if (exitTransform == null)
            {
                message = "Entrance has no exit point.";
                return false;
            }

            var agent = context.Agent;
            var character = context.Character;
            if (agent == null || character == null)
            {
                return false;
            }

            PlayerRef playerRef = agent.Object != null ? agent.Object.InputAuthority : PlayerRef.None;
            if (playerRef == PlayerRef.None)
            {
                return false;
            }

            entrance.RequestLoadingScene(playerRef);

            var controller = character.CharacterController;
            if (controller != null)
            {
                controller.SetPosition(exitTransform.position);
                controller.SetLookRotation(exitTransform.rotation);
            }

            agent.transform.SetPositionAndRotation(exitTransform.position, exitTransform.rotation);

            _networkedDungeon.ScheduleLoadingSceneHide(playerRef);

            return true;
        }

        private void Awake()
        {
            CacheNetworkedDungeon();
            UpdateInteractionCollider();
        }

        public override void Spawned()
        {
            base.Spawned();
            CacheNetworkedDungeon();
            UpdateInteractionCollider();
        }

        public override void Render()
        {
            base.Render();
            UpdateInteractionCollider();
        }

        private bool CacheNetworkedDungeon()
        {
            if (_networkedDungeon != null)
            {
                TrySyncNetworkedDungeonRef();
                return true;
            }

            if (_networkedDungeonRef.TryGet(out var cachedDungeon) && cachedDungeon != null)
            {
                _networkedDungeon = cachedDungeon;
                return true;
            }

            _networkedDungeon = GetComponentInParent<NetworkedDungeon>();
            if (_networkedDungeon == null)
            {
                _networkedDungeon = NetworkedDungeon.FindOwner(transform);
            }

            if (_networkedDungeon != null)
            {
                TrySyncNetworkedDungeonRef();
                return true;
            }

            return false;
        }

        private void UpdateInteractionCollider()
        {
            if (_interactionCollider == null)
            {
                return;
            }

            bool shouldEnable = CacheNetworkedDungeon();
            if (_interactionCollider.enabled != shouldEnable)
            {
                _interactionCollider.enabled = shouldEnable;
            }
        }

        private void TrySyncNetworkedDungeonRef()
        {
            if (_networkedDungeon == null || Object == null || Object.HasStateAuthority == false)
            {
                return;
            }

            if (_networkedDungeonRef.TryGet(out var existing) && existing == _networkedDungeon)
            {
                return;
            }

            _networkedDungeonRef = _networkedDungeon;
        }

        private void HandleNetworkedDungeonChanged()
        {
            if (_networkedDungeonRef.TryGet(out var dungeon) && dungeon != null)
            {
                _networkedDungeon = dungeon;
            }
            else
            {
                _networkedDungeon = null;
            }

            UpdateInteractionCollider();
        }

        private static void OnNetworkedDungeonChanged(Changed<DungeonExit> changed)
        {
            changed.Behaviour.HandleNetworkedDungeonChanged();
        }
    }
}
