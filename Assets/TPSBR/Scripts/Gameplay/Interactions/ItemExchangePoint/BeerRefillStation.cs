using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public sealed class BeerRefillStation : UpgradeStation
    {
        [SerializeField]
        private int _refillCost = 1;

        private UIBeerRefillStationView _beerRefillView;
        private Agent _activeAgent;
        private bool _lastPurchaseInteractable;

        public int RefillCost => Mathf.Max(0, _refillCost);

        protected override UIView _uiView => GetBeerRefillView();

        private UIBeerRefillStationView GetBeerRefillView()
        {
            if (_beerRefillView == null && Context != null && Context.UI != null)
            {
                _beerRefillView = Context.UI.Get<UIBeerRefillStationView>();
            }

            return _beerRefillView;
        }

        protected override bool ConfigureExchangeView(Agent agent, UIView view)
        {
            if (view is UIBeerRefillStationView beerView)
            {
                beerView.Configure(agent, destination => PopulateBeerItems(agent, destination));
                return true;
            }

            return base.ConfigureExchangeView(agent, view);
        }

        protected override void SubscribeToViewEvents(UIView view)
        {
            base.SubscribeToViewEvents(view);

            if (view is UIBeerRefillStationView beerView)
            {
                beerView.ItemSelected += HandleBeerItemSelected;

                if (beerView.PurchaseButton != null)
                {
                    beerView.PurchaseButton.onClick.RemoveListener(HandlePurchaseButtonClicked);
                    beerView.PurchaseButton.onClick.AddListener(HandlePurchaseButtonClicked);
                }
            }
        }

        protected override void UnsubscribeFromViewEvents(UIView view)
        {
            if (view is UIBeerRefillStationView beerView)
            {
                beerView.ItemSelected -= HandleBeerItemSelected;

                if (beerView.PurchaseButton != null)
                {
                    beerView.PurchaseButton.onClick.RemoveListener(HandlePurchaseButtonClicked);
                }
            }

            base.UnsubscribeFromViewEvents(view);
        }

        protected override void OnExchangeViewOpened(UIView view, Agent agent)
        {
            base.OnExchangeViewOpened(view, agent);

            _activeAgent = agent;
            _lastPurchaseInteractable = false;
            SetPurchaseButtonState(false);
            UpdatePurchaseButtonState();
        }

        protected override void OnExchangeViewClosed(UIView view)
        {
            SetPurchaseButtonState(false);
            _activeAgent = null;
            _lastPurchaseInteractable = false;

            base.OnExchangeViewClosed(view);
        }

        public override void Render()
        {
            base.Render();

            UpdatePurchaseButtonState();
        }

        public void RequestRefill(Agent agent)
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

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
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

        private void HandleBeerItemSelected(UpgradeStation.ItemData data)
        {
            _ = data;
            UpdatePurchaseButtonState();
        }

        private void HandlePurchaseButtonClicked()
        {
            if (_activeAgent == null)
                return;

            RequestRefill(_activeAgent);
        }

        private void ProcessRefill(Agent agent)
        {
            if (HasStateAuthority == false)
                return;

            if (TryResolveSelectedBeer(agent, out BeerUsable beer, out Inventory inventory) == false)
                return;

            if (beer.IsBeerStackFull == true)
                return;

            int cost = RefillCost;
            if (inventory.TrySpendGold(cost) == false)
                return;

            beer.AddBeerStack((byte)1);
        }

        private void UpdatePurchaseButtonState()
        {
            if (ActiveUIView != _beerRefillView)
                return;

            bool shouldBeInteractable = CanPurchase();
            if (_lastPurchaseInteractable == shouldBeInteractable)
                return;

            SetPurchaseButtonState(shouldBeInteractable);
            _lastPurchaseInteractable = shouldBeInteractable;
        }

        private void SetPurchaseButtonState(bool interactable)
        {
            if (_beerRefillView == null || _beerRefillView.PurchaseButton == null)
                return;

            _beerRefillView.PurchaseButton.interactable = interactable;
        }

        private bool CanPurchase()
        {
            if (_activeAgent == null)
                return false;

            if (TryResolveSelectedBeer(_activeAgent, out BeerUsable beer, out Inventory inventory) == false)
                return false;

            if (beer.IsBeerStackFull == true)
                return false;

            return inventory != null && inventory.Gold >= RefillCost;
        }

        private bool TryResolveSelectedBeer(Agent agent, out BeerUsable beer, out Inventory inventory)
        {
            beer = null;
            inventory = null;

            if (agent == null)
                return false;

            inventory = agent.Inventory;
            if (inventory == null)
                return false;

            if (TryGetSelectedHotbarWeapon(agent, out Weapon weapon) == false)
                return false;

            beer = weapon as BeerUsable;
            return beer != null;
        }

        private ItemStatus PopulateBeerItems(Agent agent, List<ItemData> destination)
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

            int inventorySize = inventory.InventorySize;
            for (int i = 0; i < inventorySize; ++i)
            {
                InventorySlot slot = inventory.GetItemSlot(i);
                if (slot.IsEmpty == true)
                    continue;

                if (slot.GetDefinition() is BeerDefinition beerDefinition && beerDefinition.WeaponPrefab != null)
                {
                    Sprite icon = beerDefinition.IconSprite;
                    destination.Add(new ItemData(icon, slot.Quantity, ItemSourceType.Inventory, i, beerDefinition, null));
                    hasAny = true;
                }
            }

            int hotbarSize = inventory.HotbarSize;
            for (int i = 0; i < hotbarSize; ++i)
            {
                Weapon weapon = inventory.GetHotbarWeapon(i);
                if (weapon == null)
                    continue;

                if (weapon.Definition is BeerDefinition beerDefinition)
                {
                    Sprite icon = weapon.Icon;
                    if (icon == null)
                    {
                        icon = beerDefinition.IconSprite;
                    }

                    destination.Add(new ItemData(icon, 1, ItemSourceType.Hotbar, i, beerDefinition, weapon));
                    hasAny = true;
                }
            }

            if (hasAny == false)
                return ItemStatus.NoItems;

            return ItemStatus.Success;
        }
    }
}
