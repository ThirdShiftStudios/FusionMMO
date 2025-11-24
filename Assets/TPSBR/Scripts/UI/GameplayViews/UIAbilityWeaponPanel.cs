using TMPro;
using TPSBR;
using TPSBR.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public sealed class UIAbilityWeaponPanel : MonoBehaviour
    {
        [SerializeField]
        private UIBehaviour _root;
        [SerializeField]
        private TextMeshProUGUI _slotLabel;
        [SerializeField]
        private TextMeshProUGUI _weaponNameLabel;
        [SerializeField]
        private Image _weaponIcon;
        [SerializeField]
        private UIBehaviour _equippedHighlight;
        [SerializeField]
        private UIAbilitySlotDisplay _primarySlot;
        [SerializeField]
        private UIAbilitySlotDisplay _secondarySlot;
        [SerializeField]
        private UIAbilitySlotDisplay _abilitySlot;

        private StaffWeapon _weapon;

        public StaffWeapon Weapon => _weapon;

        public void SetWeapon(Weapon weapon, int slotNumber, bool isEquipped)
        {
            UIExtensions.SetTextSafe(_slotLabel, slotNumber > 0 ? slotNumber.ToString() : string.Empty);

            _weapon = weapon as StaffWeapon;

            _root?.SetActive(weapon != null);
            _equippedHighlight?.SetActive(isEquipped);

            if (weapon == null)
            {
                SetWeaponVisuals(null, string.Empty);
                ClearAbilitySlots();
                return;
            }

            SetWeaponVisuals(weapon.Icon, weapon.DisplayName);

            if (_primarySlot != null)
            {
                _primarySlot.SetControlLabel("LMB");
            }
            if (_secondarySlot != null)
            {
                _secondarySlot.SetControlLabel("RMB");
            }
            if (_abilitySlot != null)
            {
                _abilitySlot.SetControlLabel("Q");
            }

            if (_weapon != null)
            {
                SetAbility(_primarySlot, _weapon.GetAssignedAbility(StaffWeapon.AbilityControlSlot.Primary));
                SetAbility(_secondarySlot, _weapon.GetAssignedAbility(StaffWeapon.AbilityControlSlot.Secondary));
                SetAbility(_abilitySlot, _weapon.GetAssignedAbility(StaffWeapon.AbilityControlSlot.Ability));
            }
            else
            {
                ClearAbilitySlots();
            }
        }

        public void Tick(float deltaTime)
        {
            _primarySlot?.Tick(deltaTime);
            _secondarySlot?.Tick(deltaTime);
            _abilitySlot?.Tick(deltaTime);
        }

        public bool TryStartCast(StaffWeapon.AbilityCastState castState)
        {
            if (_weapon == null || castState.Weapon != _weapon)
            {
                return false;
            }

            UIAbilitySlotDisplay targetSlot = ResolveSlotDisplay(castState.Slot);
            if (targetSlot == null || targetSlot.Ability != castState.Ability)
            {
                return false;
            }

            targetSlot.StartCast(castState.CastTime);
            return true;
        }

        private void SetAbility(UIAbilitySlotDisplay slot, AbilityDefinition ability)
        {
            slot?.SetAbility(ability);
        }

        private void ClearAbilitySlots()
        {
            _primarySlot?.SetAbility(null);
            _secondarySlot?.SetAbility(null);
            _abilitySlot?.SetAbility(null);
        }

        private UIAbilitySlotDisplay ResolveSlotDisplay(StaffWeapon.AbilityControlSlot slot)
        {
            switch (slot)
            {
                case StaffWeapon.AbilityControlSlot.Primary:
                    return _primarySlot;
                case StaffWeapon.AbilityControlSlot.Secondary:
                    return _secondarySlot;
                case StaffWeapon.AbilityControlSlot.Ability:
                    return _abilitySlot;
            }

            return null;
        }

        private void SetWeaponVisuals(Sprite icon, string displayName)
        {
            if (_weaponIcon != null)
            {
                _weaponIcon.sprite = icon;
                _weaponIcon.enabled = icon != null;
            }

            UIExtensions.SetTextSafe(_weaponNameLabel, displayName);
        }
    }
}
