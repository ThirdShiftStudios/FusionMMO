using System;
using System.Collections.Generic;
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

                private UIBeerRefillStationView _beerRefillView;
                private Agent _activeAgent;
                private Inventory _activeInventory;
                private BeerUsable _displayedBeer;
                private Transform _displayedBeerOriginalParent;
                private Vector3 _displayedBeerOriginalLocalPosition;
                private Quaternion _displayedBeerOriginalLocalRotation;
                private Vector3 _displayedBeerOriginalLocalScale;
                private ItemData? _lastSelectedItem;

                private static DataDefinition[] _cachedBeerDefinitions;
                private static readonly List<BeerUsable> _beerLookupBuffer = new List<BeerUsable>();

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
                protected override bool ShouldUseWeaponPreview => false;

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
                        UpdatePurchaseButtonState();

                        return false;
                }

                protected override void OnExchangeViewClosed(UIView view)
                {
                        RestoreDisplayedBeer();
                        base.OnExchangeViewClosed(view);

                        UnsubscribeFromInventory();
                        _activeAgent = null;
                        UpdatePurchaseButtonState();
                        _lastSelectedItem = null;
                }

                protected override void OnDisable()
                {
                        RestoreDisplayedBeer();
                        base.OnDisable();
                        _lastSelectedItem = null;
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
                        MaintainDisplayedBeer();
                }

                protected override void OnItemSelected(ItemData data)
                {
                        SetLastSelectedItem(data);
                        UpdateDisplayedBeer(data);
                }

                private void HandleItemSelectedForRefill(UpgradeStation.ItemData data)
                {
                        SetLastSelectedItem(data);
                        UpdateDisplayedBeer(data);
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

                        if (TryGetSelectedBeer(inventory, out BeerUsable beer) == false)
                                return;

                        if (beer.BeerStack >= byte.MaxValue)
                                return;

                        if (RefillCost > 0 && inventory.TrySpendGold(RefillCost) == false)
                                return;

                        beer.AddBeerStack(1);

                        UpdatePurchaseButtonState();
                }

                private bool TryGetSelectedBeer(Inventory inventory, out BeerUsable beer)
                {
                        beer = null;

                        if (inventory == null)
                                return false;

                        Weapon weapon = null;

                        if (_currentSelectedSourceType == ItemSourceType.Hotbar)
                        {
                                weapon = inventory.GetHotbarWeapon(_currentSelectedSourceIndex);
                        }
                        else if (_currentSelectedSourceType == ItemSourceType.Inventory)
                        {
                                InventorySlot slot = inventory.GetItemSlot(_currentSelectedSourceIndex);
                                ItemDefinition definition = slot.GetDefinition();

                                if (definition is ItemDefinition weaponDefinition)
                                {
                                        int hotbarSize = inventory.HotbarSize;

                                        for (int i = 0; i < hotbarSize; ++i)
                                        {
                                                Weapon candidate = inventory.GetHotbarWeapon(i);

                                                if (candidate != null && candidate.Definition == weaponDefinition)
                                                {
                                                        weapon = candidate;
                                                        break;
                                                }
                                        }
                                }
                        }

                        beer = weapon as BeerUsable;
                        return beer != null;
                }

                private void UpdateDisplayedBeer(UpgradeStation.ItemData data)
                {
                        Inventory inventory = _activeAgent != null ? _activeAgent.Inventory : null;

                        if (WeaponViewTransform == null || inventory == null)
                        {
                                RestoreDisplayedBeer();
                                return;
                        }

                        if (TryGetBeerForSelection(inventory, data, out BeerUsable beer) == false)
                        {
                                RestoreDisplayedBeer();
                                return;
                        }

                        if (_displayedBeer == beer)
                        {
                                if (beer != null && beer.transform.parent != WeaponViewTransform)
                                {
                                        Transform beerTransform = beer.transform;
                                        beerTransform.SetParent(WeaponViewTransform, false);
                                        beerTransform.localPosition = Vector3.zero;
                                        beerTransform.localRotation = Quaternion.identity;
                                        beerTransform.localScale = Vector3.one;
                                }

                                return;
                        }

                        RestoreDisplayedBeer();
                        AttachBeerToView(beer);
                }

                private void MaintainDisplayedBeer()
                {
                        Inventory inventory = _activeAgent != null ? _activeAgent.Inventory : null;

                        if (WeaponViewTransform == null || inventory == null)
                        {
                                RestoreDisplayedBeer();
                                return;
                        }

                        RefreshLastSelectedItemFromNetwork(inventory);

                        if (_lastSelectedItem.HasValue == false)
                        {
                                RestoreDisplayedBeer();
                                return;
                        }

                        ItemData data = _lastSelectedItem.Value;

                        if (TryGetBeerForSelection(inventory, data, out BeerUsable beer) == false)
                        {
                                RestoreDisplayedBeer();
                                return;
                        }

                        if (_displayedBeer == beer)
                        {
                                if (beer != null && beer.transform.parent != WeaponViewTransform)
                                {
                                        Transform beerTransform = beer.transform;
                                        beerTransform.SetParent(WeaponViewTransform, false);
                                        beerTransform.localPosition = Vector3.zero;
                                        beerTransform.localRotation = Quaternion.identity;
                                        beerTransform.localScale = Vector3.one;
                                }

                                return;
                        }

                        RestoreDisplayedBeer();
                        AttachBeerToView(beer);
                }

                private bool TryGetBeerForSelection(Inventory inventory, UpgradeStation.ItemData data, out BeerUsable beer)
                {
                        beer = null;

                        if (inventory == null)
                                return false;

                        if (data.SourceType == ItemSourceType.Hotbar)
                        {
                                beer = inventory.GetHotbarWeapon(data.SourceIndex) as BeerUsable;

                                if (beer != null)
                                        return true;
                        }

                        WeaponDefinition definition = data.Definition;

                        if (definition == null)
                                return false;

                        int hotbarSize = inventory.HotbarSize;

                        for (int i = 0; i < hotbarSize; ++i)
                        {
                                Weapon candidate = inventory.GetHotbarWeapon(i);

                                if (candidate is BeerUsable candidateBeer && candidateBeer.Definition == definition)
                                {
                                        beer = candidateBeer;
                                        return true;
                                }
                        }

                        if (_activeAgent != null)
                        {
                                _beerLookupBuffer.Clear();
                                _activeAgent.GetComponentsInChildren(true, _beerLookupBuffer);

                                for (int i = 0; i < _beerLookupBuffer.Count; ++i)
                                {
                                        BeerUsable candidate = _beerLookupBuffer[i];

                                        if (candidate != null && candidate.Definition == definition)
                                        {
                                                beer = candidate;
                                                break;
                                        }
                                }

                                _beerLookupBuffer.Clear();
                        }

                        return beer != null;
                }

                private void AttachBeerToView(BeerUsable beer)
                {
                        if (beer == null || WeaponViewTransform == null)
                                return;

                        Transform beerTransform = beer.transform;

                        _displayedBeer = beer;
                        _displayedBeerOriginalParent = beerTransform.parent;
                        _displayedBeerOriginalLocalPosition = beerTransform.localPosition;
                        _displayedBeerOriginalLocalRotation = beerTransform.localRotation;
                        _displayedBeerOriginalLocalScale = beerTransform.localScale;

                        beerTransform.SetParent(WeaponViewTransform, false);
                        beerTransform.localPosition = Vector3.zero;
                        beerTransform.localRotation = Quaternion.identity;
                        beerTransform.localScale = Vector3.one;
                }

                private void RestoreDisplayedBeer()
                {
                        if (_displayedBeer == null)
                                return;

                        Transform beerTransform = _displayedBeer.transform;

                        if (_displayedBeerOriginalParent != null)
                        {
                                beerTransform.SetParent(_displayedBeerOriginalParent, false);
                                beerTransform.localPosition = _displayedBeerOriginalLocalPosition;
                                beerTransform.localRotation = _displayedBeerOriginalLocalRotation;
                                beerTransform.localScale = _displayedBeerOriginalLocalScale;
                        }
                        else
                        {
                                beerTransform.SetParent(null, false);
                        }

                        _displayedBeer = null;
                        _displayedBeerOriginalParent = null;
                }

                private void SetLastSelectedItem(ItemData data)
                {
                        _lastSelectedItem = data;
                        _currentSelectedSourceType = data.SourceType;
                        _currentSelectedSourceIndex = data.SourceIndex;
                }

                private void RefreshLastSelectedItemFromNetwork(Inventory inventory)
                {
                        if (inventory == null)
                        {
                                _lastSelectedItem = null;
                                return;
                        }

                        if (_currentSelectedSourceType == ItemSourceType.None)
                        {
                                _lastSelectedItem = null;
                                return;
                        }

                        if (SelectedItemDefinitionId == 0)
                        {
                                if (_lastSelectedItem.HasValue == false || _lastSelectedItem.Value.Definition == null)
                                {
                                        _lastSelectedItem = null;
                                }

                                return;
                        }

                        if (_lastSelectedItem.HasValue == true)
                        {
                                ItemData current = _lastSelectedItem.Value;

                                if (current.SourceType == _currentSelectedSourceType &&
                                    current.SourceIndex == _currentSelectedSourceIndex &&
                                    current.Definition != null &&
                                    current.Definition.ID == SelectedItemDefinitionId)
                                {
                                        return;
                                }
                        }

                        WeaponDefinition definition = ItemDefinition.Get(SelectedItemDefinitionId) as WeaponDefinition;

                        if (definition == null)
                        {
                                _lastSelectedItem = null;
                                return;
                        }

                        _lastSelectedItem = new ItemData(null, 0, _currentSelectedSourceType, _currentSelectedSourceIndex, definition, null);
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

                        if (TryGetSelectedBeer(inventory, out BeerUsable beer) == false)
                                return false;

                        return beer.BeerStack < byte.MaxValue;
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
