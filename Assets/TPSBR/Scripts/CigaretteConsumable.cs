using System;
using System.Globalization;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class CigaretteConsumable : Weapon, IConsumableUse
    {
        private const string ConfigurationPrefix = "cigarette:";

        [Networked]
        private byte _cigaretteStack { get; set; }

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

        public byte CigaretteStack => _cigaretteStack;

        internal static byte GetCigaretteStack(NetworkString<_32> configurationHash)
        {
            return ParseCigaretteStack(configurationHash.ToString());
        }

        internal static NetworkString<_32> CreateConfigurationHash(byte cigaretteStack)
        {
            string configuration = string.Concat(ConfigurationPrefix, cigaretteStack.ToString(CultureInfo.InvariantCulture));

            NetworkString<_32> hash = default;
            hash = configuration;

            return hash;
        }

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

            if (_cigaretteStack == 0)
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
                if (_cigaretteStack > 0)
                {
                    byte previousStack = _cigaretteStack;
                    _cigaretteStack--;

                    if (_cigaretteStack != previousStack)
                    {
                        UpdateConfigurationHash();
                    }
                }

                Character character = Character;
                Agent agent = character != null ? character.Agent : null;

                if (_cigaretteBuffDefinition != null)
                {
                    BuffSystem buffSystem = agent != null ? agent.BuffSystem : null;
                    buffSystem?.ApplyBuff(_cigaretteBuffDefinition);
                }
            }
        }

        public void AddCigaretteStack(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (HasStateAuthority == false)
            {
                return;
            }

            int newValue = Mathf.Clamp(_cigaretteStack + amount, 0, byte.MaxValue);
            byte previousStack = _cigaretteStack;
            _cigaretteStack = (byte)newValue;

            if (_cigaretteStack != previousStack)
            {
                UpdateConfigurationHash();
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

        protected override void OnConfigurationHashApplied(string configurationHash)
        {
            base.OnConfigurationHashApplied(configurationHash);

            _cigaretteStack = ParseCigaretteStack(configurationHash);
        }

        private static byte ParseCigaretteStack(string configurationHash)
        {
            if (string.IsNullOrEmpty(configurationHash) == true)
            {
                return 0;
            }

            if (configurationHash.StartsWith(ConfigurationPrefix, StringComparison.OrdinalIgnoreCase) == false)
            {
                return 0;
            }

            string valueText = configurationHash.Substring(ConfigurationPrefix.Length);

            if (byte.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte value) == false)
            {
                return 0;
            }

            return value;
        }

        private void UpdateConfigurationHash()
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            NetworkString<_32> newHash = CreateConfigurationHash(_cigaretteStack);

            if (ConfigurationHash.Equals(newHash) == true)
            {
                return;
            }

            SetConfigurationHash(newHash);
        }

        Weapon IConsumableUse.OwnerWeapon => this;

        Character IConsumableUse.Character => Character;

        void IConsumableUse.NotifyUseFinished()
        {
            NotifyUseFinished();
        }
    }
}
