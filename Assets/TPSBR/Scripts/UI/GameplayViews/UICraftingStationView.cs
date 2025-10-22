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

                private CraftingStation _station;
                private Agent _agent;

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

                        _station = null;
                        _agent = null;
                        _recipes.Clear();

                        if (_recipeList != null)
                        {
                                _recipeList.Refresh(0);
                        }
                }

                public void Configure(CraftingStation station, Agent agent)
                {
                        _station = station;
                        _agent = agent;

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
        }
}
