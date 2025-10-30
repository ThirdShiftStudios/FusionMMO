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
                private int _refillCost = 10;

                private UIBeerRefillStationView _beerRefillView;
                private Agent _activeAgent;

                public int RefillCost => _refillCost;

                protected override UIView _uiView => GetBeerRefillView();

                protected override void SubscribeToViewEvents(UIView view)
                {
                        base.SubscribeToViewEvents(view);

                        if (view is UIBeerRefillStationView beerView)
                        {
                                beerView.PurchaseRequested -= HandlePurchaseRequested;
                                beerView.PurchaseRequested += HandlePurchaseRequested;
                        }
                }

                protected override void UnsubscribeFromViewEvents(UIView view)
                {
                        if (view is UIBeerRefillStationView beerView)
                        {
                                beerView.PurchaseRequested -= HandlePurchaseRequested;
                        }

                        base.UnsubscribeFromViewEvents(view);
                }

                protected override void OnExchangeViewOpened(UIView view, Agent agent)
                {
                        base.OnExchangeViewOpened(view, agent);
                        _activeAgent = agent;
                }

                protected override void OnExchangeViewClosed(UIView view)
                {
                        base.OnExchangeViewClosed(view);
                        _activeAgent = null;
                }

                protected override ItemStatus PopulateItems(Agent agent, List<ItemData> destination)
                {
                        if (destination == null)
                                return ItemStatus.NoItems;

                        destination.Clear();

                        if (agent == null)
                                return ItemStatus.NoAgent;

                        Inventory inventory = agent.Inventory;
                        if (inventory == null)
                                return ItemStatus.NoInventory;

                        bool hasAny = false;

                        int hotbarSize = inventory.HotbarSize;
                        for (int i = 0; i < hotbarSize; ++i)
                        {
                                Weapon weapon = inventory.GetHotbarWeapon(i);
                                if (weapon is not BeerUsable beer)
                                        continue;

                                if (beer.Definition is not BeerDefinition definition)
                                        continue;

                                Sprite icon = weapon.Icon;
                                if (icon == null && definition != null)
                                {
                                        icon = definition.IconSprite;
                                }

                                destination.Add(new ItemData(icon, beer.BeerStack, ItemSourceType.Hotbar, i, definition, beer));
                                hasAny = true;
                        }

                        if (hasAny == false)
                                return ItemStatus.NoItems;

                        return ItemStatus.Success;
                }

                protected override bool MatchesFilter(DataDefinition definition)
                {
                        return definition is BeerDefinition;
                }

                private UIBeerRefillStationView GetBeerRefillView()
                {
                        if (_beerRefillView == null && Context != null && Context.UI != null)
                        {
                                _beerRefillView = Context.UI.Get<UIBeerRefillStationView>();
                        }

                        return _beerRefillView;
                }

                private void HandlePurchaseRequested()
                {
                        NetworkId agentId = _activeAgent != null ? _activeAgent.Object.Id : NetworkId.Invalid;
                        RequestPurchaseRefill(agentId);
                }

                private void RequestPurchaseRefill(NetworkId agentId)
                {
                        if (HasStateAuthority == true)
                        {
                                PurchaseRefill(agentId);
                        }
                        else
                        {
                                RPC_RequestPurchaseRefill(agentId);
                        }
                }

                [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
                private void RPC_RequestPurchaseRefill(NetworkId agentId)
                {
                        PurchaseRefill(agentId);
                }

                private void PurchaseRefill(NetworkId agentId)
                {
                        Agent agent = ResolveAgent(agentId);
                        if (agent == null)
                                return;

                        if (TryGetSelectedBeer(agent, out BeerUsable beer, out Inventory inventory) == false)
                                return;

                        if (beer.IsStackFull == true)
                                return;

                        bool spentGold = false;

                        if (_refillCost > 0)
                        {
                                if (inventory.Gold < _refillCost)
                                        return;

                                if (inventory.TrySpendGold(_refillCost) == false)
                                        return;

                                spentGold = true;
                        }

                        if (beer.TryAddBeerStack(1) == false)
                        {
                                if (spentGold == true)
                                {
                                        inventory.AddGold(_refillCost);
                                }
                        }
                }

                private Agent ResolveAgent(NetworkId agentId)
                {
                        if (agentId.IsValid == false)
                                return null;

                        if (Runner != null && Runner.TryFindObject(agentId, out NetworkObject agentObject) == true)
                        {
                                return agentObject.GetComponent<Agent>();
                        }

                        return null;
                }

                private bool TryGetSelectedBeer(Agent agent, out BeerUsable beer, out Inventory inventory)
                {
                        beer = null;
                        inventory = null;

                        if (agent == null)
                                return false;

                        inventory = agent.Inventory;
                        if (inventory == null)
                                return false;

                        if (CurrentSelectedSourceType != ItemSourceType.Hotbar)
                                return false;

                        int index = CurrentSelectedSourceIndex;
                        if (index < 0 || index >= inventory.HotbarSize)
                                return false;

                        Weapon weapon = inventory.GetHotbarWeapon(index);
                        if (weapon is not BeerUsable beerUsable)
                                return false;

                        if (beerUsable.Definition is not BeerDefinition)
                                return false;

                        beer = beerUsable;
                        return true;
                }

#if UNITY_EDITOR
                private void OnValidate()
                {
                        _refillCost = Mathf.Max(0, _refillCost);
                }
#endif
        }
}
