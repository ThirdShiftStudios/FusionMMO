using Fusion;
using TPSBR.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public sealed class UIAbilityView : UIWidget
    {
        [SerializeField]
        private UIWeaponAbilityPanel _equippedPanel;
        [SerializeField]
        private UIWeaponAbilityPanel _unequippedPanel;
        [SerializeField]
        private Color _castOverlayColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField]
        private float _overlayRotationSpeed = 120f;
        [SerializeField]
        private GameObject _selectCastRoot;
        [SerializeField]
        private Image _selectCastIcon;
        [SerializeField]
        private TextMeshProUGUI _selectCastLabel;
        [SerializeField]
        private string _selectPromptTemplate = "LMB to cast {0}";

        public void UpdateAbilities(Agent agent)
        {
            Inventory inventory = agent != null ? agent.Inventory : null;
            bool inventorySpawned = inventory != null && inventory.Runner != null && inventory.Object != null && inventory.Runner.Exists(inventory.Object) == true;

            StaffWeapon equipped = inventorySpawned == true ? inventory.CurrentWeapon as StaffWeapon : null;
            int equippedSlot = equipped != null && inventorySpawned == true ? inventory.CurrentWeaponSlot : -1;

            ResolveSecondaryWeapon(inventory, equippedSlot, inventorySpawned, out StaffWeapon secondary, out int secondarySlot);

            UpdatePanel(_equippedPanel, equipped, equippedSlot);
            UpdatePanel(_unequippedPanel, secondary, secondarySlot);

            UpdateSelectCastPrompt(equipped, secondary);
        }

        public void Clear()
        {
            _equippedPanel?.Clear();
            _unequippedPanel?.Clear();
            UpdateSelectCastPrompt(null, null);
        }

        private void UpdatePanel(UIWeaponAbilityPanel panel, StaffWeapon weapon, int slotIndex)
        {
            if (panel == null)
            {
                return;
            }

            StaffAbilityDefinition castingAbility = null;
            StaffWeapon.AbilityControlSlot castingSlot = StaffWeapon.AbilityControlSlot.Primary;
            float castProgress = 0f;

            if (weapon != null && weapon.TryGetActiveCast(out StaffAbilityDefinition activeAbility, out StaffWeapon.AbilityControlSlot activeSlot, out float progress) == true)
            {
                castingAbility = activeAbility;
                castingSlot = activeSlot;
                castProgress = progress;
            }

            panel.Display(weapon, slotIndex, Time.deltaTime, castingAbility, castingSlot, castProgress, _castOverlayColor, _overlayRotationSpeed);
        }

        private void ResolveSecondaryWeapon(Inventory inventory, int equippedSlot, bool inventorySpawned, out StaffWeapon secondary, out int slotIndex)
        {
            secondary = null;
            slotIndex = -1;

            if (inventory == null || inventorySpawned == false)
            {
                return;
            }

            int previousSlot = inventory.PreviousWeaponSlot;
            if (previousSlot >= 0 && previousSlot != equippedSlot)
            {
                secondary = inventory.GetHotbarWeapon(previousSlot) as StaffWeapon;
                slotIndex = previousSlot;
            }

            if (secondary != null)
            {
                return;
            }

            int hotbarSize = inventory.HotbarSize;
            for (int i = 0; i < hotbarSize; ++i)
            {
                if (i == equippedSlot)
                {
                    continue;
                }

                StaffWeapon candidate = inventory.GetHotbarWeapon(i) as StaffWeapon;
                if (candidate != null)
                {
                    secondary = candidate;
                    slotIndex = i;
                    return;
                }
            }
        }

        private void UpdateSelectCastPrompt(StaffWeapon equipped, StaffWeapon secondary)
        {
            StaffAbilityDefinition selectAbility = equipped != null && equipped.IsSelectCastActive == true
                ? equipped.ActiveSelectCastAbility
                : null;

            if (selectAbility == null && secondary != null && secondary.IsSelectCastActive == true)
            {
                selectAbility = secondary.ActiveSelectCastAbility;
            }

            if (_selectCastRoot != null)
            {
                _selectCastRoot.SetActive(selectAbility != null);
            }

            if (selectAbility == null)
            {
                if (_selectCastIcon != null)
                {
                    _selectCastIcon.enabled = false;
                    _selectCastIcon.sprite = null;
                }

                if (_selectCastLabel != null)
                {
                    _selectCastLabel.text = string.Empty;
                }

                return;
            }

            if (_selectCastIcon != null)
            {
                _selectCastIcon.sprite = selectAbility.Icon;
                _selectCastIcon.enabled = _selectCastIcon.sprite != null;
            }

            if (_selectCastLabel != null)
            {
                string prompt = string.IsNullOrWhiteSpace(_selectPromptTemplate) == false
                    ? string.Format(_selectPromptTemplate, selectAbility.Name)
                    : $"LMB to cast {selectAbility.Name}";

                UIExtensions.SetTextSafe(_selectCastLabel, prompt);
            }
        }
    }
}
