using System.Collections.Generic;
using TSS.Data;
using UnityEngine;

namespace TPSBR.UI
{
    [DisallowMultipleComponent]
    public sealed class UIAlchemyDropContainer : MonoBehaviour
    {
        public AlchemyCategory Category => _category;
        public UIListItem DropSlot => _dropSlot;
        public IReadOnlyList<UIAlchemyStationView.InventoryEntry> Items => _items;

        [SerializeField]
        private AlchemyCategory _category;
        [SerializeField]
        private UIListItem _dropSlot;
        [SerializeField]
        private UIList _itemList;
        [SerializeField]
        private GameObject _highlightBorder;

        private readonly List<UIAlchemyStationView.InventoryEntry> _items = new List<UIAlchemyStationView.InventoryEntry>();

        internal void Initialize(IUIListItemOwner owner, int dropIndex)
        {
            if (_dropSlot != null)
            {
                _dropSlot.InitializeSlot(owner, dropIndex);
            }

            if (_itemList != null)
            {
                _itemList.UpdateContent -= HandleUpdateItemContent;
                _itemList.UpdateContent += HandleUpdateItemContent;
                _itemList.Refresh(_items.Count);
            }

            SetHighlight(false);
        }

        public bool Accepts(ItemDefinition definition)
        {
            if (definition == null)
                return false;

            return _category switch
            {
                AlchemyCategory.Flora => definition is FloraResource,
                AlchemyCategory.Essence => definition is EssenceResource,
                AlchemyCategory.Ore => definition is OreResource,
                AlchemyCategory.BaseLiquid => definition is BaseLiquid,
                _ => false,
            };
        }

        public void AddItem(UIAlchemyStationView.InventoryEntry entry)
        {
            _items.Add(entry);
            RefreshList();
        }

        public void ClearItems()
        {
            _items.Clear();
            RefreshList();
        }

        public void SetHighlight(bool active)
        {
            if (_highlightBorder != null)
            {
                _highlightBorder.SetActive(active);
            }
        }

        private void RefreshList()
        {
            if (_itemList != null)
            {
                _itemList.Refresh(_items.Count);
            }
        }

        private void HandleUpdateItemContent(int index, MonoBehaviour content)
        {
            if (index < 0 || index >= _items.Count)
                return;

            UIAlchemyStationView.InventoryEntry entry = _items[index];

            if (content is UIListItem listItem)
            {
                listItem.SetItem(entry.Icon, entry.Quantity);
            }
        }
    }
}
