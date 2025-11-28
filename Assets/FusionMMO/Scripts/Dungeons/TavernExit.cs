using Fusion;
using UnityEngine;
using FusionMMO.Dungeons;

namespace TPSBR
{
    public sealed class TavernExit : ContextBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField] private string _interactionName = "Leave Tavern";
        [SerializeField, TextArea] private string _interactionDescription = "Return to the entrance.";
        [SerializeField] private Transform _hudPivot;
        [SerializeField] private Collider _interactionCollider;

        [Header("References")]
        [SerializeField] private TavernNetworked _networkedTavern;

        [Networked]
        [OnChangedRender(nameof(OnNetworkedTavernIdChanged))]
        private NetworkBehaviourId _networkedTavernId { get; set; }

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

            if (CacheNetworkedTavern() == false)
            {
                message = "No tavern found.";
                return false;
            }

            var entrance = _networkedTavern.Entrance;
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

            _networkedTavern.ScheduleLoadingSceneHide(playerRef);

            return true;
        }

        private void Awake()
        {
            CacheNetworkedTavern();
            UpdateInteractionCollider();
        }

        public override void Spawned()
        {
            base.Spawned();
            CacheNetworkedTavern();
            UpdateInteractionCollider();
        }

        public override void Render()
        {
            base.Render();
            UpdateInteractionCollider();
        }

        private bool CacheNetworkedTavern()
        {
            if (_networkedTavern != null)
            {
                TrySyncNetworkedTavernId();
                return true;
            }

            _networkedTavern = GetComponentInParent<TavernNetworked>();
            if (_networkedTavern == null)
            {
                if (TryResolveNetworkedTavernId())
                {
                    return true;
                }
            }

            if (_networkedTavern != null)
            {
                TrySyncNetworkedTavernId();
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

            bool shouldEnable = CacheNetworkedTavern();
            if (_interactionCollider.enabled != shouldEnable)
            {
                _interactionCollider.enabled = shouldEnable;
            }
        }

        private bool TryResolveNetworkedTavernId()
        {
            if (Runner == null || _networkedTavernId.IsValid == false)
            {
                return false;
            }

            if (Runner.TryFindBehaviour(_networkedTavernId, out TavernNetworked tavern) && tavern != null)
            {
                if (_networkedTavern != tavern)
                {
                    _networkedTavern = tavern;
                }

                return true;
            }

            return false;
        }

        private void TrySyncNetworkedTavernId()
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

            if (_networkedTavern == null)
            {
                if (_networkedTavernId.IsValid)
                {
                    _networkedTavernId = default;
                }

                return;
            }

            NetworkBehaviourId id = runner.TryGetNetworkedBehaviourId(_networkedTavern);
            if (id.IsValid == false)
            {
                if (_networkedTavernId.IsValid)
                {
                    _networkedTavernId = default;
                }

                return;
            }

            if (_networkedTavernId != id)
            {
                _networkedTavernId = id;
            }
        }

        private void OnNetworkedTavernIdChanged()
        {
            if (TryResolveNetworkedTavernId() == false)
            {
                _networkedTavern = null;
            }

            UpdateInteractionCollider();
        }
    }
}
