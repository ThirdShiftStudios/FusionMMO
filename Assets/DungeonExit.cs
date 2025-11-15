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

        [Networked]
        [OnChangedRender(nameof(OnNetworkedDungeonIdChanged))]
        private NetworkBehaviourId _networkedDungeonId { get; set; }

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
                TrySyncNetworkedDungeonId();
                return true;
            }

            _networkedDungeon = GetComponentInParent<NetworkedDungeon>();
            if (_networkedDungeon == null)
            {
                if (TryResolveNetworkedDungeonId())
                {
                    return true;
                }

                _networkedDungeon = NetworkedDungeon.FindOwner(transform);
            }

            if (_networkedDungeon != null)
            {
                TrySyncNetworkedDungeonId();
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

        private bool TryResolveNetworkedDungeonId()
        {
            if (Runner == null || _networkedDungeonId.IsValid == false)
            {
                return false;
            }

            if (Runner.TryFindBehaviour(_networkedDungeonId, out NetworkedDungeon dungeon) && dungeon != null)
            {
                if (_networkedDungeon != dungeon)
                {
                    _networkedDungeon = dungeon;
                }

                return true;
            }

            return false;
        }

        private void TrySyncNetworkedDungeonId()
        {
            if (Object == null || Object.HasStateAuthority == false)
            {
                return;
            }

            var runner = Runner;
            if (runner == null)
            {
                return;
            }

            if (_networkedDungeon == null)
            {
                if (_networkedDungeonId.IsValid)
                {
                    _networkedDungeonId = default;
                }

                return;
            }

            if (runner.TryGetNetworkedBehaviourId(_networkedDungeon, out var id) == false)
            {
                if (_networkedDungeonId.IsValid)
                {
                    _networkedDungeonId = default;
                }

                return;
            }

            if (_networkedDungeonId != id)
            {
                _networkedDungeonId = id;
            }
        }

        private void OnNetworkedDungeonIdChanged()
        {
            if (TryResolveNetworkedDungeonId() == false)
            {
                _networkedDungeon = null;
            }

            UpdateInteractionCollider();
        }
    }
}
