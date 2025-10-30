using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
    public sealed class UICraftingStationView : UICloseView
    {
        public const string ResourcePath = "UI/GameplayViews/UICraftingStationView";

        [SerializeField]
        private UIList _recipeList;

        [SerializeField]
        private RectTransform _listContainer;

        private readonly List<RecipeDefinition> _recipes = new List<RecipeDefinition>();
        private readonly Dictionary<RecipeDefinition, UIRecipeListItem> _recipeItems = new Dictionary<RecipeDefinition, UIRecipeListItem>();

        private CraftingStation _station;
        private Agent _agent;
        private RecipeDefinition _activeRecipe;
        private float _activeCraftStartTime;
        private float _activeCraftDuration;
        private int _activeCraftQuantity;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_recipeList != null)
            {
                _recipeList.UpdateContent -= HandleUpdateRecipeContent;
                _recipeList.UpdateContent += HandleUpdateRecipeContent;
            }
        }

        protected override void OnDeinitialize()
        {
            if (_recipeList != null)
            {
                _recipeList.UpdateContent -= HandleUpdateRecipeContent;
            }

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            RefreshRecipeList();
            SubscribeToInventory();
        }

        protected override void OnClose()
        {
            base.OnClose();

            UnsubscribeFromInventory();

            if (_station != null)
            {
                _station.CraftStarted -= HandleCraftStarted;
                _station.CraftCompleted -= HandleCraftCompleted;
                _station.CraftCancelled -= HandleCraftCancelled;
            }

            _station = null;
            _agent = null;
            ClearActiveCraftState();
            _recipes.Clear();

            foreach (KeyValuePair<RecipeDefinition, UIRecipeListItem> pair in _recipeItems)
            {
                UIRecipeListItem item = pair.Value;
                if (item == null)
                    continue;

                item.CancelRequested -= HandleCancelRequested;
                item.CraftRequested -= HandleCraftRequested;
                item.CraftAllRequested -= HandleCraftAllRequested;
            }

            _recipeItems.Clear();

            if (_recipeList != null)
            {
                _recipeList.Refresh(0);
            }
        }

        public void Configure(CraftingStation station, Agent agent)
        {
            if (_station != null)
            {
                _station.CraftStarted -= HandleCraftStarted;
                _station.CraftCompleted -= HandleCraftCompleted;
                _station.CraftCancelled -= HandleCraftCancelled;
            }

            _station = station;
            _agent = agent;

            if (_station != null)
            {
                _station.CraftStarted -= HandleCraftStarted;
                _station.CraftStarted += HandleCraftStarted;
                _station.CraftCompleted -= HandleCraftCompleted;
                _station.CraftCompleted += HandleCraftCompleted;
                _station.CraftCancelled -= HandleCraftCancelled;
                _station.CraftCancelled += HandleCraftCancelled;
                SyncActiveCraftState();
            }
            else
            {
                ClearActiveCraftState();
            }

            _recipes.Clear();

            if (station != null)
            {
                IReadOnlyList<RecipeDefinition> definitions = station.Recipes;
                if (definitions != null)
                {
                    for (int i = 0; i < definitions.Count; ++i)
                    {
                        RecipeDefinition recipe = definitions[i];
                        if (recipe != null)
                        {
                            _recipes.Add(recipe);
                        }
                    }
                }
            }

            RefreshRecipeList();
            SubscribeToInventory();
        }

        private void RefreshRecipeList()
        {
            if (_recipeList == null)
                return;

            _recipeItems.Clear();
            _recipeList.Refresh(_recipes.Count);
        }

        private void SubscribeToInventory()
        {
            Inventory inventory = _agent != null ? _agent.Inventory : null;
            if (inventory == null)
                return;

            inventory.ItemSlotChanged -= HandleInventoryChanged;
            inventory.ItemSlotChanged += HandleInventoryChanged;
        }

        private void UnsubscribeFromInventory()
        {
            if (_agent == null)
                return;

            Inventory inventory = _agent.Inventory;
            if (inventory == null)
                return;

            inventory.ItemSlotChanged -= HandleInventoryChanged;
        }

        private void HandleInventoryChanged(int index, InventorySlot slot)
        {
            _ = index;
            _ = slot;

            RefreshRecipeList();
        }

        private void HandleUpdateRecipeContent(int index, MonoBehaviour content)
        {
            if (index < 0 || index >= _recipes.Count)
                return;

            RecipeDefinition recipe = _recipes[index];
            if (recipe == null)
                return;

            if (content is UIRecipeListItem recipeItem)
            {
                int craftableCount = 0;
                if (_station != null && _agent != null)
                {
                    craftableCount = _station.GetCraftableCount(_agent, recipe);
                }

                recipeItem.Configure(recipe, craftableCount);
                recipeItem.CraftRequested -= HandleCraftRequested;
                recipeItem.CraftRequested += HandleCraftRequested;
                recipeItem.CraftAllRequested -= HandleCraftAllRequested;
                recipeItem.CraftAllRequested += HandleCraftAllRequested;
                recipeItem.CancelRequested -= HandleCancelRequested;
                recipeItem.CancelRequested += HandleCancelRequested;

                _recipeItems[recipe] = recipeItem;
                ApplyCraftState(recipe, recipeItem);
            }
        }

        private void HandleCraftRequested(RecipeDefinition recipe)
        {
            if (recipe == null || _agent == null || _station == null)
                return;

            _station.RequestCraft(_agent, recipe, 1);
        }

        private void HandleCraftAllRequested(RecipeDefinition recipe)
        {
            if (recipe == null || _agent == null || _station == null)
                return;

            int craftable = _station.GetCraftableCount(_agent, recipe);
            if (craftable <= 1)
            {
                _station.RequestCraft(_agent, recipe, Mathf.Max(1, craftable));
                return;
            }

            _station.RequestCraft(_agent, recipe, craftable);
        }

        private void HandleCancelRequested(RecipeDefinition recipe)
        {
            if (recipe == null || _agent == null || _station == null)
                return;

            if (_activeRecipe != null && _activeRecipe != recipe)
                return;

            _station.RequestCancelCraft(_agent);
        }

        private void HandleCraftStarted(RecipeDefinition recipe, float startTime, float duration, int quantity)
        {
            if (recipe == null)
                return;

            RecipeDefinition previous = _activeRecipe;

            _activeRecipe = recipe;
            _activeCraftStartTime = startTime;
            _activeCraftDuration = duration;
            _activeCraftQuantity = quantity;

            if (previous != null && previous != recipe && _recipeItems.TryGetValue(previous, out UIRecipeListItem previousItem) == true)
            {
                previousItem.ApplyCraftingState(false, 0f, 0f);
            }

            if (_activeRecipe != null && _recipeItems.TryGetValue(_activeRecipe, out UIRecipeListItem activeItem) == true)
            {
                activeItem.ApplyCraftingState(true, _activeCraftStartTime, _activeCraftDuration);
            }
        }

        private void HandleCraftCompleted(RecipeDefinition recipe)
        {
            if (_activeRecipe != null && _recipeItems.TryGetValue(_activeRecipe, out UIRecipeListItem item) == true)
            {
                item.ApplyCraftingState(false, 0f, 0f);
            }

            ClearActiveCraftState();
            RefreshRecipeList();
        }

        private void HandleCraftCancelled(RecipeDefinition recipe)
        {
            if (_activeRecipe != null && _recipeItems.TryGetValue(_activeRecipe, out UIRecipeListItem item) == true)
            {
                item.ApplyCraftingState(false, 0f, 0f);
            }

            ClearActiveCraftState();
            RefreshRecipeList();
        }

        private void ApplyCraftState(RecipeDefinition recipe, UIRecipeListItem item)
        {
            if (item == null)
                return;

            bool isActive = _activeRecipe != null && _activeRecipe == recipe;

            if (isActive == true)
            {
                item.ApplyCraftingState(true, _activeCraftStartTime, _activeCraftDuration);
            }
            else
            {
                item.ApplyCraftingState(false, 0f, 0f);
            }
        }

        private void SyncActiveCraftState()
        {
            if (_station != null && _station.TryGetLocalCraftState(out RecipeDefinition recipe, out float startTime, out float duration, out int quantity) == true)
            {
                _activeRecipe = recipe;
                _activeCraftStartTime = startTime;
                _activeCraftDuration = duration;
                _activeCraftQuantity = quantity;
            }
            else
            {
                ClearActiveCraftState();
            }
        }

        private void ClearActiveCraftState()
        {
            _activeRecipe = null;
            _activeCraftStartTime = 0f;
            _activeCraftDuration = 0f;
            _activeCraftQuantity = 0;
        }
    }
}
