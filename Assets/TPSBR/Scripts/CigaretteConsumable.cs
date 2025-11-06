using Fusion;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class CigaretteConsumable : Weapon, IConsumableUse
    {
        private bool _isSmoking;

        [SerializeField]
        private Renderer[] _renderers;
        [SerializeField]
        private Collider[] _colliders;
        [SerializeField]
        private CigaretteBuff _cigaretteBuffDefinition;

        private bool _renderersResolved;
        private bool _collidersResolved;
        private bool _previewVisible;

        public byte CigaretteStack => GetInventoryCigaretteStack();

        private void Awake()
        {
            ResolveRenderers();
            ResolveColliders();
            SetVisualsVisible(false);
        }

        private void OnEnable()
        {
            UpdateVisualState();
        }

        private void OnDisable()
        {
            SetVisualsVisible(false);
        }

        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
        }

        public override WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            if (_isSmoking == true)
            {
                return WeaponUseRequest.None;
            }

            if (attackActivated == false)
            {
                return WeaponUseRequest.None;
            }

            if (CigaretteStack == 0)
            {
                return WeaponUseRequest.None;
            }

            return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.CigaretteSmoke);
        }

        public override void OnUseStarted(in WeaponUseRequest request)
        {
            if (request.Animation == WeaponUseAnimation.CigaretteSmoke)
            {
                _isSmoking = true;
            }
        }

        protected override void OnWeaponArmed()
        {
            base.OnWeaponArmed();
            UpdateVisualState();
        }

        protected override void OnWeaponDisarmed()
        {
            base.OnWeaponDisarmed();
            UpdateVisualState();
        }

        public override bool HandleAnimationRequest(UseLayer attackLayer, in WeaponUseRequest request)
        {
            if (request.Animation != WeaponUseAnimation.CigaretteSmoke)
            {
                return base.HandleAnimationRequest(attackLayer, request);
            }

            if (attackLayer == null)
            {
                return false;
            }

            CigaretteUseState cigaretteUseState = attackLayer.CigaretteUseState;

            if (cigaretteUseState == null)
            {
                return false;
            }

            cigaretteUseState.PlaySmoke(this);

            return true;
        }

        internal void NotifyUseFinished()
        {
            _isSmoking = false;

            if (HasStateAuthority == true)
            {
                bool cigaretteConsumed = false;

                if (TryGetInventorySlot(out Inventory inventory, out int slotIndex, out InventorySlot slot) == true && slot.Quantity > 0)
                {
                    cigaretteConsumed = inventory.TryConsumeInventoryItem(slotIndex, 1);
                }

                if (cigaretteConsumed == true && _cigaretteBuffDefinition != null)
                {
                    Character character = Character;
                    Agent agent = character != null ? character.Agent : null;
                    BuffSystem buffSystem = agent != null ? agent.BuffSystem : null;
                    buffSystem?.ApplyBuff(_cigaretteBuffDefinition);
                }
            }
        }

        public void SetPreviewVisibility(bool visible)
        {
            if (_previewVisible == visible)
            {
                return;
            }

            _previewVisible = visible;

            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (gameObject.activeInHierarchy == false)
            {
                return;
            }

            SetVisualsVisible(IsArmed || _previewVisible);
        }

        private void ResolveRenderers()
        {
            if (_renderersResolved == true)
            {
                return;
            }

            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            _renderersResolved = true;
        }

        private void ResolveColliders()
        {
            if (_collidersResolved == true)
            {
                return;
            }

            if (_colliders == null || _colliders.Length == 0)
            {
                _colliders = GetComponentsInChildren<Collider>(true);
            }

            _collidersResolved = true;
        }

        private void SetVisualsVisible(bool visible)
        {
            ResolveRenderers();
            ResolveColliders();

            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    var renderer = _renderers[i];
                    if (renderer != null)
                    {
                        renderer.enabled = visible;
                    }
                }
            }

            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    var collider = _colliders[i];
                    if (collider != null)
                    {
                        collider.enabled = visible;
                    }
                }
            }
        }

        Weapon IConsumableUse.OwnerWeapon => this;

        Character IConsumableUse.Character => Character;

        void IConsumableUse.NotifyUseFinished()
        {
            NotifyUseFinished();
        }

        private byte GetInventoryCigaretteStack()
        {
            if (TryGetInventorySlot(out _, out _, out InventorySlot slot) == true)
            {
                return slot.Quantity;
            }

            return 0;
        }

        private bool TryGetInventorySlot(out Inventory inventory, out int slotIndex, out InventorySlot slot)
        {
            Character character = Character;
            Agent agent = character != null ? character.Agent : null;
            inventory = agent != null ? agent.Inventory : null;

            slotIndex = -1;
            slot = default;

            if (inventory == null)
            {
                return false;
            }

            WeaponDefinition definition = Definition;
            if (definition == null)
            {
                return false;
            }

            if (inventory.TryFindInventorySlot(definition, out slotIndex, out slot) == true)
            {
                return true;
            }

            inventory = null;
            slotIndex = -1;
            slot = default;
            return false;
        }
    }
}
