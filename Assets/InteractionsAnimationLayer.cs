using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class InteractionsAnimationLayer : AnimationLayer
    {
        public enum InteractionType
        {
            None,
            OpenChest,
        }

        [SerializeField]
        private OpenChestState _openChest;

        private InteractionType _activeInteraction = InteractionType.None;

        public OpenChestState OpenChest
        {
            get
            {
                if (_openChest == null)
                {
                    FindState(out _openChest, true);
                }

                return _openChest;
            }
        }

        public InteractionType ActiveInteraction => _activeInteraction;
        public bool HasActiveInteraction => _activeInteraction != InteractionType.None;

        public bool TryBeginInteraction(InteractionType interactionType)
        {
            if (interactionType == InteractionType.None)
                return false;

            if (_activeInteraction != InteractionType.None)
                return false;

            _activeInteraction = interactionType;
            return true;
        }

        public void EndInteraction(InteractionType interactionType)
        {
            if (_activeInteraction == interactionType)
            {
                _activeInteraction = InteractionType.None;
            }
        }
    }
}
