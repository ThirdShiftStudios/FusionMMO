using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class BeerUsable : Weapon
    {
        [Networked]
        private byte _beerStack { get; set; }

        private bool _isDrinking;
        [SerializeField]
        private Renderer[] _renderers;
        [SerializeField]
        private Collider[] _colliders;
        [SerializeField]
        private DrunkBuffDefinition _drunkBuffDefinition;

        private bool _renderersResolved;
        private bool _collidersResolved;
        private bool _previewVisible;

        public byte BeerStack => _beerStack;

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
            if (_isDrinking == true)
            {
                return WeaponUseRequest.None;
            }

            if (attackActivated == false)
            {
                return WeaponUseRequest.None;
            }

            if (_beerStack == 0)
            {
                return WeaponUseRequest.None;
            }

            return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.BeerDrink);
        }

        public override void OnUseStarted(in WeaponUseRequest request)
        {
            if (request.Animation == WeaponUseAnimation.BeerDrink)
            {
                _isDrinking = true;
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
            if (request.Animation != WeaponUseAnimation.BeerDrink)
            {
                return base.HandleAnimationRequest(attackLayer, request);
            }

            if (attackLayer == null)
            {
                return false;
            }

            BeerUseState beerUseState = attackLayer.BeerUseState;

            if (beerUseState == null)
            {
                return false;
            }

            beerUseState.PlayDrink(this);

            return true;
        }

        internal void NotifyDrinkFinished()
        {
            _isDrinking = false;

            if (HasStateAuthority == true)
            {
                if (_beerStack > 0)
                {
                    _beerStack--;
                }

                Character character = Character;
                Agent agent = character != null ? character.Agent : null;

                if (_drunkBuffDefinition != null)
                {
                    BuffSystem buffSystem = agent != null ? agent.BuffSystem : null;
                    buffSystem?.ApplyBuff(_drunkBuffDefinition);
                }
            }
        }

        public void AddBeerStack(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (HasStateAuthority == false)
            {
                return;
            }

            int newValue = Mathf.Clamp(_beerStack + amount, 0, byte.MaxValue);
            _beerStack = (byte)newValue;
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
    }
}
