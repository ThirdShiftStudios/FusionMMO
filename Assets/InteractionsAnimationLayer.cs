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
            MineOre,
            HarvestGlyph,
        }

        [SerializeField]
        private OpenChestState _openChest;
        [SerializeField]
        private MineOreState _mineOre;
        [SerializeField]
        private HarvestGlyphState _harvestGlyph;

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

        public MineOreState MineOre
        {
            get
            {
                if (_mineOre == null)
                {
                    FindState(out _mineOre, true);
                }

                return _mineOre;
            }
        }

        public HarvestGlyphState HarvestGlyph
        {
            get
            {
                if (_harvestGlyph == null)
                {
                    FindState(out _harvestGlyph, true);
                }

                return _harvestGlyph;
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
