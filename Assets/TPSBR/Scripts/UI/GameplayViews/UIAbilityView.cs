using System.Collections.Generic;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using TPSBR;
using TPSBR.Abilities;

namespace TPSBR.UI
{
    public sealed class UIAbilityView : UIWidget
    {
        [SerializeField]
        private UIAbilityWeaponPanel _primaryWeaponPanel;
        [SerializeField]
        private UIAbilityWeaponPanel _secondaryWeaponPanel;
        [SerializeField]
        private UIBehaviour _selectCastRoot;
        [SerializeField]
        private Image _selectCastIcon;
        [SerializeField]
        private TextMeshProUGUI _selectCastLabel;

        private Agent _agent;
        private Inventory _inventory;
        private Weapon _lastPrimary;
        private Weapon _lastSecondary;
        private int _lastSelectedSlot = -1;
        private AbilityDefinition _lastSelectCastAbility;
        private readonly HashSet<StaffWeapon> _subscribedWeapons = new HashSet<StaffWeapon>();

        public void BindAgent(Agent agent)
        {
            if (_agent == agent)
            {
                return;
            }

            UnsubscribeWeapons();

            _agent = agent;
            _inventory = agent != null ? agent.Inventory : null;
            _lastPrimary = null;
            _lastSecondary = null;
            _lastSelectedSlot = -1;
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();

            BindAgent(null);
        }

        protected override void OnTick()
        {
            base.OnTick();

            float delta = Time.deltaTime;
            _primaryWeaponPanel?.Tick(delta);
            _secondaryWeaponPanel?.Tick(delta);

            RefreshWeaponPanels();
            RefreshSelectCastPrompt();
        }

        private void RefreshWeaponPanels()
        {
            if (_inventory == null)
            {
                _primaryWeaponPanel?.SetWeapon(null, 0, false);
                _secondaryWeaponPanel?.SetWeapon(null, 0, false);
                return;
            }

            int primaryIndex = Inventory.HOTBAR_PRIMARY_WEAPON_SLOT;
            int secondaryIndex = Inventory.HOTBAR_SECONDARY_WEAPON_SLOT;

            Weapon primaryWeapon = _inventory.GetHotbarWeapon(primaryIndex);
            Weapon secondaryWeapon = _inventory.GetHotbarWeapon(secondaryIndex);
            int currentSlot = _inventory.CurrentWeaponSlot;

            bool needsUpdate = primaryWeapon != _lastPrimary || secondaryWeapon != _lastSecondary || currentSlot != _lastSelectedSlot;

            if (needsUpdate == true)
            {
                _primaryWeaponPanel?.SetWeapon(primaryWeapon, primaryIndex + 1, currentSlot == primaryIndex);
                _secondaryWeaponPanel?.SetWeapon(secondaryWeapon, secondaryIndex + 1, currentSlot == secondaryIndex);

                _lastPrimary = primaryWeapon;
                _lastSecondary = secondaryWeapon;
                _lastSelectedSlot = currentSlot;

                UpdateWeaponSubscriptions(primaryWeapon as StaffWeapon, secondaryWeapon as StaffWeapon);
            }
        }

        private void RefreshSelectCastPrompt()
        {
            if (_selectCastRoot == null)
            {
                return;
            }

            StaffWeapon staffWeapon = _inventory != null ? _inventory.CurrentWeapon as StaffWeapon : null;
            AbilityDefinition selectCastAbility = staffWeapon != null && staffWeapon.IsSelectCastActive == true
                ? staffWeapon.ActiveSelectCastAbility
                : null;

            if (selectCastAbility == null)
            {
                _selectCastRoot.SetActive(false);
                _lastSelectCastAbility = null;
                return;
            }

            if (_lastSelectCastAbility != selectCastAbility)
            {
                _lastSelectCastAbility = selectCastAbility;

                if (_selectCastIcon != null)
                {
                    _selectCastIcon.sprite = selectCastAbility.Icon;
                    _selectCastIcon.enabled = _selectCastIcon.sprite != null;
                }

                string abilityName = string.IsNullOrWhiteSpace(selectCastAbility.Name) == false
                    ? selectCastAbility.Name
                    : "ability";

                UIExtensions.SetTextSafe(_selectCastLabel, $"LMB to cast {abilityName}");
            }

            _selectCastRoot.SetActive(true);
        }

        private void UpdateWeaponSubscriptions(params StaffWeapon[] weapons)
        {
            var desired = new HashSet<StaffWeapon>();

            if (weapons != null)
            {
                for (int i = 0; i < weapons.Length; ++i)
                {
                    StaffWeapon weapon = weapons[i];
                    if (weapon == null)
                    {
                        continue;
                    }

                    desired.Add(weapon);

                    if (_subscribedWeapons.Contains(weapon) == false)
                    {
                        weapon.AbilityCastStarted += HandleAbilityCastStarted;
                        _subscribedWeapons.Add(weapon);
                    }
                }
            }

            var toRemove = new List<StaffWeapon>(_subscribedWeapons);
            for (int i = 0; i < toRemove.Count; ++i)
            {
                StaffWeapon weapon = toRemove[i];
                if (desired.Contains(weapon) == false)
                {
                    weapon.AbilityCastStarted -= HandleAbilityCastStarted;
                    _subscribedWeapons.Remove(weapon);
                }
            }
        }

        private void UnsubscribeWeapons()
        {
            foreach (StaffWeapon weapon in _subscribedWeapons)
            {
                if (weapon != null)
                {
                    weapon.AbilityCastStarted -= HandleAbilityCastStarted;
                }
            }

            _subscribedWeapons.Clear();
        }

        private void HandleAbilityCastStarted(StaffWeapon.AbilityCastState castState)
        {
            if (_primaryWeaponPanel != null && _primaryWeaponPanel.TryStartCast(castState) == true)
            {
                return;
            }

            _secondaryWeaponPanel?.TryStartCast(castState);
        }
    }
}
