using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TPSBR.UI
{
    using ArcaneConduit = TPSBR.ArcaneConduit;
    using StaffWeapon = TPSBR.StaffWeapon;
    using TPSBR.Abilities;

    public sealed class UIAbilityControlSlot : MonoBehaviour
    {
        [SerializeField]
        private StaffWeapon.AbilityControlSlot _slotType;
        [SerializeField]
        private UIListItem _slot;
        [SerializeField]
        private TextMeshProUGUI _controlLabel;
        [SerializeField]
        private TextMeshProUGUI _abilityLabel;
        [SerializeField]
        private Image _abilityIcon;
        [SerializeField]
        private string _emptyAbilityText = "Empty";

        private ArcaneConduit.AbilityOption? _assignedOption;

        public StaffWeapon.AbilityControlSlot SlotType => _slotType;
        public UIListItem Slot
        {
            get
            {
                if (_slot == null)
                {
                    _slot = GetComponent<UIListItem>();
                }

                return _slot;
            }
        }

        public ArcaneConduit.AbilityOption? AssignedOption => _assignedOption;

        private void Awake()
        {
            if (_slot == null)
            {
                _slot = GetComponent<UIListItem>();
            }
        }

        public void SetControlLabel(string label)
        {
            UIExtensions.SetTextSafe(_controlLabel, label);
        }

        public void SetAssignedAbility(ArcaneConduit.AbilityOption? option)
        {
            _assignedOption = option;

            if (option.HasValue == false)
            {
                UIExtensions.SetTextSafe(_abilityLabel, _emptyAbilityText);

                if (_abilityIcon != null)
                {
                    _abilityIcon.sprite = null;
                    _abilityIcon.enabled = false;
                }

                return;
            }

            ArcaneConduit.AbilityOption abilityOption = option.Value;
            AbilityDefinition definition = abilityOption.Definition;

            string abilityName = definition != null ? definition.Name : string.Empty;
            if (string.IsNullOrWhiteSpace(abilityName) == true)
            {
                abilityName = _emptyAbilityText;
            }

            UIExtensions.SetTextSafe(_abilityLabel, abilityName);

            if (_abilityIcon != null)
            {
                Sprite icon = definition != null ? definition.Icon : null;
                _abilityIcon.sprite = icon;
                _abilityIcon.enabled = icon != null;
            }
        }

        public void ClearAssignedAbility()
        {
            SetAssignedAbility(null);
        }
    }
}
