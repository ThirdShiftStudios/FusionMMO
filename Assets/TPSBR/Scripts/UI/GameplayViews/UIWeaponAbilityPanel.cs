using TPSBR.Abilities;
using TMPro;
using UnityEngine;

namespace TPSBR.UI
{
    public sealed class UIWeaponAbilityPanel : MonoBehaviour
    {
        [SerializeField]
        private GameObject _root;
        [SerializeField]
        private TextMeshProUGUI _weaponLabel;
        [SerializeField]
        private UIAbilityIconDisplay[] _abilitySlots;
        [SerializeField]
        private UIAbilityIconDisplay _primarySlot;
        [SerializeField]
        private UIAbilityIconDisplay _secondarySlot;
        [SerializeField]
        private UIAbilityIconDisplay _abilitySlot;

        private StaffWeapon _weapon;
        private int _hotbarIndex = -1;

        public void Display(StaffWeapon weapon, int hotbarIndex, float deltaTime, StaffAbilityDefinition castingAbility, StaffWeapon.AbilityControlSlot castingSlot, float castProgress, Color overlayColor, float rotationSpeed)
        {
            _weapon = weapon;
            _hotbarIndex = hotbarIndex;

            RefreshSlots();
            ApplyCastState(castingAbility, castingSlot, castProgress, deltaTime, overlayColor, rotationSpeed);
        }

        public void Clear()
        {
            Display(null, -1, 0f, null, StaffWeapon.AbilityControlSlot.Primary, 0f, Color.clear, 0f);
        }

        private void RefreshSlots()
        {
            bool hasWeapon = _weapon != null;

            if (_root != null)
            {
                _root.SetActive(hasWeapon);
            }

            if (_weaponLabel != null)
            {
                string weaponName = hasWeapon == true ? _weapon.DisplayName : "";

                if (hasWeapon == true && _hotbarIndex >= 0)
                {
                    weaponName = string.IsNullOrWhiteSpace(weaponName) == false
                        ? $"{weaponName} [{_hotbarIndex}]"
                        : $"Slot {_hotbarIndex}";
                }

                UIExtensions.SetTextSafe(_weaponLabel, weaponName);
            }

            if (_abilitySlots != null)
            {
                for (int i = 0; i < _abilitySlots.Length; ++i)
                {
                    UIAbilityIconDisplay slot = _abilitySlots[i];
                    if (slot == null)
                        continue;

                    StaffAbilityDefinition ability = ResolveConfiguredAbility(i);
                    string label = hasWeapon == true ? (i + 1).ToString() : string.Empty;

                    slot.SetAbility(ability, label);
                }
            }

            SetControlSlot(_primarySlot, StaffWeapon.AbilityControlSlot.Primary, "LMB");
            SetControlSlot(_secondarySlot, StaffWeapon.AbilityControlSlot.Secondary, "RMB");
            SetControlSlot(_abilitySlot, StaffWeapon.AbilityControlSlot.Ability, "Q");
        }

        private void ApplyCastState(StaffAbilityDefinition castingAbility, StaffWeapon.AbilityControlSlot castingSlot, float castProgress, float deltaTime, Color overlayColor, float rotationSpeed)
        {
            bool highlightCast = castingAbility != null && castProgress > 0f;

            if (_abilitySlots != null)
            {
                for (int i = 0; i < _abilitySlots.Length; ++i)
                {
                    UIAbilityIconDisplay slot = _abilitySlots[i];
                    if (slot == null)
                        continue;

                    bool isCasting = highlightCast == true && slot.Ability == castingAbility;
                    slot.UpdateCastProgress(isCasting ? castProgress : 0f, deltaTime, overlayColor, rotationSpeed);
                }
            }

            UpdateControlCast(_primarySlot, castingAbility, StaffWeapon.AbilityControlSlot.Primary, castingSlot, castProgress, deltaTime, overlayColor, rotationSpeed);
            UpdateControlCast(_secondarySlot, castingAbility, StaffWeapon.AbilityControlSlot.Secondary, castingSlot, castProgress, deltaTime, overlayColor, rotationSpeed);
            UpdateControlCast(_abilitySlot, castingAbility, StaffWeapon.AbilityControlSlot.Ability, castingSlot, castProgress, deltaTime, overlayColor, rotationSpeed);
        }

        private void SetControlSlot(UIAbilityIconDisplay target, StaffWeapon.AbilityControlSlot slot, string label)
        {
            StaffAbilityDefinition ability = ResolveAssignedAbility(slot);
            target?.SetAbility(ability, label);
        }

        private void UpdateControlCast(UIAbilityIconDisplay target, StaffAbilityDefinition castingAbility, StaffWeapon.AbilityControlSlot castSlot, StaffWeapon.AbilityControlSlot castingSlot, float castProgress, float deltaTime, Color overlayColor, float rotationSpeed)
        {
            if (target == null)
            {
                return;
            }

            bool isCasting = castingAbility != null && castingSlot == castSlot && target.Ability == castingAbility;
            target.UpdateCastProgress(isCasting ? castProgress : 0f, deltaTime, overlayColor, rotationSpeed);
        }

        private StaffAbilityDefinition ResolveConfiguredAbility(int index)
        {
            if (_weapon == null || _weapon.ConfiguredAbilities == null)
            {
                return null;
            }

            if (index < 0 || index >= _weapon.ConfiguredAbilities.Count)
            {
                return null;
            }

            return _weapon.ConfiguredAbilities[index];
        }

        private StaffAbilityDefinition ResolveAssignedAbility(StaffWeapon.AbilityControlSlot slot)
        {
            if (_weapon == null)
            {
                return null;
            }

            var assignments = _weapon.AssignedAbilityIndexes;
            var configured = _weapon.ConfiguredAbilities;

            int slotIndex = (int)slot;

            if (assignments == null || configured == null || slotIndex < 0 || slotIndex >= assignments.Count)
            {
                return null;
            }

            int configuredIndex = assignments[slotIndex];
            if (configuredIndex < 0 || configuredIndex >= configured.Count)
            {
                return null;
            }

            return configured[configuredIndex];
        }
    }
}
