using System;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
        public sealed class BeerRefillStation : UpgradeStation
        {
                [SerializeField]
                private int _refillCost = 5;

                [SerializeField]
                private Animator _animator;

                [SerializeField]
                private string _fillAnimation = string.Empty;

                private UIBeerRefillStationView _beerRefillView;
                private Agent _activeAgent;
                private Inventory _activeInventory;
                private BeerUsable _previewedBeer;

                private static DataDefinition[] _cachedBeerDefinitions;

                public int RefillCost => _refillCost;

                public override void Spawned()
                {
                        base.Spawned();

                        EnsureBeerFilters();
                }

#if UNITY_EDITOR
                private void OnValidate()
                {
                        EnsureBeerFilters();
                }
#endif

                protected override UIView _uiView => GetBeerRefillStationView();

                private UIBeerRefillStationView GetBeerRefillStationView()
                {
                        if (_beerRefillView == null && Context != null && Context.UI != null)
                        {
                                _beerRefillView = Context.UI.Get<UIBeerRefillStationView>();
                        }

                        return _beerRefillView;
                }

                protected override bool ConfigureExchangeView(Agent agent, UIView view)
                {
                        bool configured = base.ConfigureExchangeView(agent, view);

                        if (configured == true)
                        {
                                _activeAgent = agent;
                                SubscribeToInventory(agent != null ? agent.Inventory : null);
                                UpdatePurchaseButtonState();
                                return true;
                        }

                        _activeAgent = null;
                        SubscribeToInventory(null);
                        SetPreviewedBeer(null);
                        UpdatePurchaseButtonState();

                        return false;
                }

                protected override void OnExchangeViewClosed(UIView view)
                {
                        base.OnExchangeViewClosed(view);

                        SetPreviewedBeer(null);
                        UnsubscribeFromInventory();
                        _activeAgent = null;
                        UpdatePurchaseButtonState();
                }

                protected override void SubscribeToViewEvents(UIView view)
                {
                        base.SubscribeToViewEvents(view);

                        if (view is UIBeerRefillStationView refillView)
                        {
                                refillView.ItemSelected += HandleItemSelectedForRefill;
                                refillView.PurchaseButtonClicked += HandlePurchaseButtonClicked;
                        }
                }

                protected override void UnsubscribeFromViewEvents(UIView view)
                {
                        if (view is UIBeerRefillStationView refillView)
                        {
                                refillView.ItemSelected -= HandleItemSelectedForRefill;
                                refillView.PurchaseButtonClicked -= HandlePurchaseButtonClicked;
                        }

                        base.UnsubscribeFromViewEvents(view);
                }

                public override void Render()
                {
                        base.Render();

                        UpdatePurchaseButtonState();
                }

                protected override void ConfigureWeaponPreview(Weapon preview)
                {
                        base.ConfigureWeaponPreview(preview);

                        UpdatePreviewStackAnimation(_previewedBeer);
                }

                protected override void OnDisable()
                {
                        SetPreviewedBeer(null);
                        base.OnDisable();
                }

                private void HandleItemSelectedForRefill(UpgradeStation.ItemData data)
                {
                        _currentSelectedSourceType = data.SourceType;
                        _currentSelectedSourceIndex = data.SourceIndex;
                        UpdatePurchaseButtonState();
                }

                private void HandlePurchaseButtonClicked()
                {
                        RequestRefill(_activeAgent);
                }

                private void RequestRefill(Agent agent)
                {
                        if (agent == null)
                                return;

                        if (HasStateAuthority == true)
                        {
                                ProcessRefill(agent);
                        }
                        else
                        {
                                RPC_RequestRefill(agent.Object.InputAuthority, agent.Object.Id);
                        }
                }

                [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
                private void RPC_RequestRefill(PlayerRef playerRef, NetworkId agentId)
                {
                        if (Runner == null)
                                return;

                        if (Runner.TryFindObject(agentId, out NetworkObject agentObject) == false)
                                return;

                        Agent agent = agentObject.GetComponent<Agent>();

                        if (agent == null)
                                return;

                        if (agent.Object == null || agent.Object.InputAuthority != playerRef)
                                return;

                        ProcessRefill(agent);
                }

                private void ProcessRefill(Agent agent)
                {
                        Inventory inventory = agent != null ? agent.Inventory : null;

                        if (inventory == null)
                                return;

                        if (TryGetSelectedBeer(inventory, out BeerUsable beer, out InventorySlot inventorySlot, out bool fromInventory) == false)
                                return;

                        bool refillSuccessful = false;

                        if (fromInventory == true)
                        {
                                byte currentStack = BeerUsable.GetBeerStack(inventorySlot.ConfigurationHash);
                                bool canIncreaseStack = currentStack < byte.MaxValue;
                                int maxStack = GetInventorySlotStackLimit(inventorySlot);
                                bool canIncreaseQuantity = inventorySlot.Quantity < maxStack;

                                if (canIncreaseStack == false && canIncreaseQuantity == false)
                                        return;

                                if (RefillCost > 0 && inventory.TrySpendGold(RefillCost) == false)
                                        return;

                                if (canIncreaseStack == true)
                                {
                                        byte newStack = (byte)Mathf.Min(byte.MaxValue, currentStack + 1);
                                        NetworkString<_64> newConfiguration = BeerUsable.CreateConfigurationHash(newStack);

                                        if (inventory.TrySetInventorySlotConfiguration(_currentSelectedSourceIndex, newConfiguration) == false)
                                        {
                                                if (RefillCost > 0)
                                                {
                                                        inventory.AddGold(RefillCost);
                                                }

                                                return;
                                        }

                                        refillSuccessful = true;
                                }
                                else if (inventory.TryAddToInventorySlot(_currentSelectedSourceIndex, 1) == false)
                                {
                                        if (RefillCost > 0)
                                        {
                                                inventory.AddGold(RefillCost);
                                        }

                                        return;
                                }
                                else
                                {
                                        refillSuccessful = true;
                                }
                        }
                        else
                        {
                                if (beer == null)
                                        return;

                                if (beer.BeerStack >= byte.MaxValue)
                                        return;

                                if (RefillCost > 0 && inventory.TrySpendGold(RefillCost) == false)
                                        return;

                                beer.AddBeerStack(1);
                                refillSuccessful = true;
                        }

                        if (refillSuccessful == true)
                        {
                                PlayFillAnimation();
                        }

                        UpdatePurchaseButtonState();
                }

                private void PlayFillAnimation()
                {
                        if (_animator == null)
                                return;

                        if (string.IsNullOrEmpty(_fillAnimation) == true)
                                return;

                        _animator.Play(_fillAnimation, 0, 0f);
                }

                private bool TryGetSelectedBeer(Inventory inventory, out BeerUsable beer, out InventorySlot inventorySlot, out bool fromInventory)
                {
                        beer = null;
                        inventorySlot = default;
                        fromInventory = false;

                        if (inventory == null)
                                return false;

                        if (_currentSelectedSourceType == ItemSourceType.Hotbar)
                        {
                                Weapon weapon = inventory.GetHotbarWeapon(_currentSelectedSourceIndex);
                                beer = weapon as BeerUsable;
                                return beer != null;
                        }

                        if (_currentSelectedSourceType == ItemSourceType.Inventory)
                        {
                                InventorySlot slot = inventory.GetItemSlot(_currentSelectedSourceIndex);

                                if (slot.IsEmpty == true)
                                        return false;

                                ItemDefinition definition = slot.GetDefinition();

                                if (definition is BeerDefinition)
                                {
                                        fromInventory = true;
                                        inventorySlot = slot;

                                        int hotbarSize = inventory.HotbarSize;

                                        for (int i = 0; i < hotbarSize; ++i)
                                        {
                                                Weapon candidate = inventory.GetHotbarWeapon(i);

                                                if (candidate is BeerUsable candidateBeer && candidate.Definition == definition)
                                                {
                                                        beer = candidateBeer;
                                                        break;
                                                }
                                        }

                                        return true;
                                }
                        }

                        return false;
                }

                private void SubscribeToInventory(Inventory inventory)
                {
                        if (_activeInventory == inventory)
                                return;

                        UnsubscribeFromInventory();

                        _activeInventory = inventory;

                        if (_activeInventory != null)
                        {
                                _activeInventory.GoldChanged -= HandleGoldChanged;
                                _activeInventory.GoldChanged += HandleGoldChanged;
                        }
                }

                private void UnsubscribeFromInventory()
                {
                        if (_activeInventory == null)
                                return;

                        _activeInventory.GoldChanged -= HandleGoldChanged;
                        _activeInventory = null;
                }

                private void HandleGoldChanged(int value)
                {
                        _ = value;
                        UpdatePurchaseButtonState();
                }

                private void UpdatePurchaseButtonState()
                {
                        if (ActiveUIView is UIBeerRefillStationView refillView)
                        {
                                refillView.SetPurchaseButtonInteractable(CanPurchase());
                        }

                        UpdateSelectedBeerPreview();
                }

                private bool CanPurchase()
                {
                        if (_activeAgent == null)
                                return false;

                        Inventory inventory = _activeAgent.Inventory;

                        if (inventory == null)
                                return false;

                        if (RefillCost > 0 && inventory.Gold < RefillCost)
                                return false;

                        if (TryGetSelectedBeer(inventory, out BeerUsable beer, out InventorySlot inventorySlot, out bool fromInventory) == false)
                                return false;

                        if (fromInventory == true)
                        {
                                byte currentStack = BeerUsable.GetBeerStack(inventorySlot.ConfigurationHash);

                                if (currentStack < byte.MaxValue)
                                        return true;

                                int maxStack = GetInventorySlotStackLimit(inventorySlot);
                                return inventorySlot.Quantity < maxStack;
                        }

                        return beer != null && beer.BeerStack < byte.MaxValue;
                }

                private void UpdateSelectedBeerPreview()
                {
                        BeerUsable selectedBeer = null;

                        Inventory inventory = _activeAgent != null ? _activeAgent.Inventory : null;

                        if (inventory != null)
                        {
                                if (TryGetSelectedBeer(inventory, out selectedBeer, out _, out _) == false)
                                {
                                        selectedBeer = null;
                                }
                        }

                        SetPreviewedBeer(selectedBeer);
                }

                private void SetPreviewedBeer(BeerUsable beer)
                {
                        if (_previewedBeer == beer)
                        {
                                UpdatePreviewStackAnimation(beer);
                                return;
                        }

                        if (_previewedBeer != null)
                        {
                                _previewedBeer.SetPreviewVisibility(false);
                        }

                        _previewedBeer = beer;

                        if (_previewedBeer != null)
                        {
                                _previewedBeer.SetPreviewVisibility(true);
                        }

                        UpdatePreviewStackAnimation(_previewedBeer);
                }

                private void UpdatePreviewStackAnimation(BeerUsable sourceBeer)
                {
                        BeerUsable beerPreview = CurrentWeaponPreview as BeerUsable;

                        if (beerPreview == null)
                                return;

                        byte stack = 0;

                        if (sourceBeer != null)
                        {
                                stack = GetSafeBeerStack(sourceBeer);
                        }
                        else
                        {
                                Inventory inventory = _activeAgent != null ? _activeAgent.Inventory : null;

                                if (inventory != null && TryGetSelectedBeer(inventory, out BeerUsable selectedBeer, out InventorySlot inventorySlot, out bool fromInventory) == true)
                                {
                                        if (fromInventory == true)
                                        {
                                                stack = BeerUsable.GetBeerStack(inventorySlot.ConfigurationHash);
                                        }
                                        else if (selectedBeer != null)
                                        {
                                                stack = GetSafeBeerStack(selectedBeer);
                                        }
                                }
                        }

                        beerPreview.SnapPreviewToStack(stack);
                }

                private static byte GetSafeBeerStack(BeerUsable beer)
                {
                        if (beer == null)
                                return 0;

                        NetworkObject networkObject = beer.Object;

                        if (networkObject == null || networkObject.IsValid == false || networkObject.Runner == null)
                                return 0;

                        return beer.BeerStack;
                }

                private static int GetInventorySlotStackLimit(InventorySlot slot)
                {
                        ushort maxStack = ItemDefinition.GetMaxStack(slot.ItemDefinitionId);

                        if (maxStack == 0)
                                return byte.MaxValue;

                        return Mathf.Clamp(maxStack, 1, byte.MaxValue);
                }

                private void EnsureBeerFilters()
                {
                        if (FilterDefinitions != null && FilterDefinitions.Length > 0)
                        {
                                for (int i = 0; i < FilterDefinitions.Length; ++i)
                                {
                                        if (FilterDefinitions[i] is BeerDefinition)
                                                return;
                                }
                        }

                        FilterDefinitions = GetBeerDefinitions();
                }

                private static DataDefinition[] GetBeerDefinitions()
                {
                        if (_cachedBeerDefinitions != null)
                                return _cachedBeerDefinitions;

                        BeerDefinition[] beerDefinitions = Resources.LoadAll<BeerDefinition>(string.Empty);

                        if (beerDefinitions == null || beerDefinitions.Length == 0)
                        {
                                _cachedBeerDefinitions = Array.Empty<DataDefinition>();
                                return _cachedBeerDefinitions;
                        }

                        DataDefinition[] result = new DataDefinition[beerDefinitions.Length];

                        for (int i = 0; i < beerDefinitions.Length; ++i)
                        {
                                result[i] = beerDefinitions[i];
                        }

                        _cachedBeerDefinitions = result;
                        return _cachedBeerDefinitions;
                }
        }
}
