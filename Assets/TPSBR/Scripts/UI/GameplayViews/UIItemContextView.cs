using System;
using System.Text;
using TMPro;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR.UI
{
        public sealed class UIItemContextView : UICloseView
        {
                [SerializeField]
                private TextMeshProUGUI _staffListLabel;
                [SerializeField]
                private string _noAgentText = "No agent available.";
                [SerializeField]
                private string _noInventoryText = "Inventory unavailable.";
                [SerializeField]
                private string _noStaffText = "No staffs available.";

                private Agent _sourceAgent;
                private readonly StringBuilder _builder = new StringBuilder(256);
                private string _lastRenderedList;

                public void SetSourceAgent(Agent agent)
                {
                        _sourceAgent = agent;
                        RefreshStaffList(true);
                }

                protected override void OnOpen()
                {
                        base.OnOpen();

                        RefreshStaffList(true);
                }

                protected override void OnClose()
                {
                        base.OnClose();

                        _lastRenderedList = null;

                        if (_staffListLabel != null)
                        {
                                UIExtensions.SetTextSafe(_staffListLabel, string.Empty);
                        }
                }

                protected override void OnTick()
                {
                        base.OnTick();

                        RefreshStaffList(false);
                }

                private void RefreshStaffList(bool force)
                {
                        if (_staffListLabel == null)
                                return;

                        string listText = BuildStaffList();

                        if (force == false && string.Equals(_lastRenderedList, listText, StringComparison.Ordinal) == true)
                                return;

                        _lastRenderedList = listText;
                        UIExtensions.SetTextSafe(_staffListLabel, listText);
                }

                private string BuildStaffList()
                {
                        if (_sourceAgent == null)
                                return _noAgentText;

                        Inventory inventory = _sourceAgent.Inventory;
                        if (inventory == null)
                                return _noInventoryText;

                        _builder.Clear();

                        bool hasAny = AppendInventoryStaffs(inventory);
                        hasAny = AppendHotbarStaffs(inventory) || hasAny;

                        if (hasAny == false)
                                return _noStaffText;

                        return _builder.ToString();
                }

                private bool AppendInventoryStaffs(Inventory inventory)
                {
                        bool hasAny = false;
                        int inventorySize = inventory.InventorySize;

                        for (int i = 0; i < inventorySize; ++i)
                        {
                                InventorySlot slot = inventory.GetItemSlot(i);
                                if (slot.IsEmpty == true)
                                        continue;

                                if (slot.GetDefinition() is WeaponDefinition weaponDefinition && IsStaffDefinition(weaponDefinition) == true)
                                {
                                        AppendLine($"Inventory Slot {i + 1}: {GetWeaponDefinitionName(weaponDefinition)}");
                                        hasAny = true;
                                }
                        }

                        return hasAny;
                }

                private bool AppendHotbarStaffs(Inventory inventory)
                {
                        bool hasAny = false;
                        int hotbarSize = inventory.HotbarSize;

                        for (int i = 0; i < hotbarSize; ++i)
                        {
                                Weapon weapon = inventory.GetHotbarWeapon(i);
                                if (weapon == null)
                                        continue;

                                if (weapon.Size != WeaponSize.Staff)
                                        continue;

                                AppendLine($"Hotbar Slot {i + 1}: {GetWeaponName(weapon)}");
                                hasAny = true;
                        }

                        return hasAny;
                }

                private void AppendLine(string value)
                {
                        if (_builder.Length > 0)
                        {
                                _builder.AppendLine();
                        }

                        _builder.Append(value);
                }

                private static string GetWeaponDefinitionName(WeaponDefinition definition)
                {
                        if (definition == null)
                                return string.Empty;

                        string name = definition.Name;
                        if (string.IsNullOrWhiteSpace(name) == false)
                                return name;

                        return definition.name;
                }

                private static string GetWeaponName(Weapon weapon)
                {
                        if (weapon == null)
                                return string.Empty;

                        string name = weapon.DisplayName;
                        if (string.IsNullOrWhiteSpace(name) == false)
                                return name;

                        return weapon.name;
                }

                private static bool IsStaffDefinition(WeaponDefinition definition)
                {
                        if (definition == null)
                                return false;

                        Weapon prefab = definition.WeaponPrefab;
                        if (prefab == null)
                                return false;

                        return prefab.Size == WeaponSize.Staff;
                }
        }
}
