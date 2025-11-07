using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIGameplayInteractions : UIBehaviour
    {
        // PRIVATE MEMBERS

        [SerializeField]
        private UIBehaviour _interactGroup;
        [SerializeField]
        private UIBehaviour _interactionInfoGroup;
        [SerializeField]
        private TextMeshProUGUI _interactionName;
        [SerializeField]
        private TextMeshProUGUI _interactionDescription;
        [SerializeField]
        private UIBehaviour _harvestProgressGroup;
        [SerializeField]
        private Image _harvestProgressFill;

        private bool _hasInteractionTarget;
        private IInteraction _interactionTarget;
        private bool _infoActive;

        // MONOBEHAVIOUR

        protected void OnEnable()
        {
            SetInteractionTarget(null, true);
            HideHarvestProgress();
        }

        // PUBLIC MEMBERS

        public void UpdateInteractions(SceneContext context, Agent agent)
        {
            var interactionTarget = agent.Interactions.InteractionTarget;
            bool force = false;

            // Interaction target could get destroyed in the meantime (+ special check due to interface required)
            if (_hasInteractionTarget == true && (interactionTarget == null || interactionTarget.Equals(null)))
            {
                interactionTarget = null;
                force = true;
            }

            SetInteractionTarget(interactionTarget, force);

            UpdateInfoPosition(context);

            UpdateHarvestProgress(agent);
        }

        // PRIVATE MEMBERS

        private void SetInteractionTarget(IInteraction interactionTarget, bool force = false)
        {
            if (interactionTarget == _interactionTarget && force == false)
                return;

            _interactionTarget = interactionTarget;
            _hasInteractionTarget = interactionTarget != null;

            _interactGroup.SetActive(_hasInteractionTarget);

            _infoActive = _hasInteractionTarget == true && interactionTarget.Name.HasValue();
            _interactionInfoGroup.SetActive(_infoActive);

            if (_infoActive == true)
            {
                _interactionName.text = interactionTarget.Name;
                _interactionDescription.text = interactionTarget.Description;
            }

            if (interactionTarget is ResourceNode == false)
            {
                HideHarvestProgress();
            }
        }

        private void UpdateInfoPosition(SceneContext context)
        {
            if (_infoActive == false)
                return;

            var screenPosition = context.Camera.Camera.WorldToScreenPoint(_interactionTarget.HUDPosition);
            _interactionInfoGroup.transform.position = screenPosition;
        }

        private void UpdateHarvestProgress(Agent agent)
        {
            bool isHarvesting = false;

            if (_harvestProgressGroup != null && _harvestProgressFill != null)
            {
                if (agent != null && _interactionTarget is ResourceNode resourceNode && resourceNode.IsInteracting(agent) == true)
                {
                    isHarvesting = true;
                    _harvestProgressGroup.SetActive(true);
                    _harvestProgressFill.fillAmount = resourceNode.InteractionProgressNormalized;
                }
                else
                {
                    HideHarvestProgress();
                }
            }

            if (_interactGroup != null)
            {
                _interactGroup.SetActive(_hasInteractionTarget && isHarvesting == false);
            }
        }

        private void HideHarvestProgress()
        {
            _harvestProgressGroup?.SetActive(false);

            if (_harvestProgressFill != null)
            {
                _harvestProgressFill.fillAmount = 0f;
            }
        }
    }
}
