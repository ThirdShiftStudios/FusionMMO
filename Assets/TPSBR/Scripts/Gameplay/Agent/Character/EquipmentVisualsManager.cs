namespace TPSBR
{
    using System.Collections.Generic;
    using UnityEngine;
    using TSS.Data;

    public sealed class EquipmentVisualsManager : MonoBehaviour
    {
        [SerializeField]
        private bool _collectOnStart = true;

        private readonly Dictionary<ESlotCategory, List<EquipmentVisual>> _visualsByCategory = new Dictionary<ESlotCategory, List<EquipmentVisual>>();
        private readonly List<EquipmentVisual> _visualBuffer = new List<EquipmentVisual>();

        private Inventory _inventory;

        private static readonly ESlotCategory[] _trackedCategories =
        {
            ESlotCategory.Head,
            ESlotCategory.UpperBody,
            ESlotCategory.LowerBody,
            ESlotCategory.Pipe,
        };

        private void Awake()
        {
            if (_collectOnStart == true)
            {
                CollectVisuals();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void Initialize(Inventory inventory)
        {
            if (_visualsByCategory.Count == 0)
            {
                CollectVisuals();
            }

            if (_inventory == inventory)
            {
                RefreshAll();
                return;
            }

            Unsubscribe();

            _inventory = inventory;

            if (_inventory != null)
            {
                _inventory.ItemSlotChanged += OnItemSlotChanged;
                RefreshAll();
            }
            else
            {
                RefreshAll();
            }
        }

        public void CollectVisuals()
        {
            _visualsByCategory.Clear();
            GetComponentsInChildren(true, _visualBuffer);

            for (int i = 0; i < _visualBuffer.Count; ++i)
            {
                EquipmentVisual visual = _visualBuffer[i];
                if (visual == null)
                {
                    continue;
                }

                ESlotCategory category = visual.SlotCategory;
                if (_visualsByCategory.TryGetValue(category, out List<EquipmentVisual> visuals) == false)
                {
                    visuals = new List<EquipmentVisual>();
                    _visualsByCategory.Add(category, visuals);
                }

                visuals.Add(visual);
            }

            _visualBuffer.Clear();
        }

        private void RefreshAll()
        {
            for (int i = 0; i < _trackedCategories.Length; ++i)
            {
                RefreshCategory(_trackedCategories[i]);
            }
        }

        private void OnItemSlotChanged(int index, InventorySlot slot)
        {
            switch (index)
            {
                case Inventory.HEAD_SLOT_INDEX:
                    RefreshCategory(ESlotCategory.Head, slot);
                    break;
                case Inventory.UPPER_BODY_SLOT_INDEX:
                    RefreshCategory(ESlotCategory.UpperBody, slot);
                    break;
                case Inventory.LOWER_BODY_SLOT_INDEX:
                    RefreshCategory(ESlotCategory.LowerBody, slot);
                    break;
                case Inventory.PIPE_SLOT_INDEX:
                    RefreshCategory(ESlotCategory.Pipe, slot);
                    break;
            }
        }

        private void RefreshCategory(ESlotCategory category)
        {
            InventorySlot slot = _inventory != null ? _inventory.GetEquipmentSlot(category) : default;
            RefreshCategory(category, slot);
        }

        private void RefreshCategory(ESlotCategory category, InventorySlot slot)
        {
            if (_visualsByCategory.TryGetValue(category, out List<EquipmentVisual> visuals) == false)
            {
                return;
            }

            ItemDefinition definition = slot.GetDefinition();

            for (int i = 0; i < visuals.Count; ++i)
            {
                EquipmentVisual visual = visuals[i];
                if (visual == null)
                {
                    continue;
                }

                bool shouldBeVisible = ShouldShowVisual(visual, definition);
                GameObject visualObject = visual.gameObject;
                if (visualObject != null && visualObject.activeSelf != shouldBeVisible)
                {
                    visualObject.SetActive(shouldBeVisible);
                }
            }
        }

        private static bool ShouldShowVisual(EquipmentVisual visual, ItemDefinition definition)
        {
            if (definition != null)
            {
                return visual.DefaultObject == false && visual.ItemDefinition == definition;
            }

            return visual.DefaultObject == true;
        }

        private void Unsubscribe()
        {
            if (_inventory != null)
            {
                _inventory.ItemSlotChanged -= OnItemSlotChanged;
                _inventory = null;
            }
        }
    }
}
