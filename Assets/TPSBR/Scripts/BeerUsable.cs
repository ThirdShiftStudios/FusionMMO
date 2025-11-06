using System;
using System.Collections;
using System.Globalization;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

namespace TPSBR
{
    public class BeerUsable : Weapon, IConsumableUse
    {
        private const string ConfigurationPrefix = "beer:";

        [Networked]
        private byte _beerStack { get; set; }

        private bool _isDrinking;
        [SerializeField]
        private Renderer[] _renderers;
        [SerializeField]
        private Collider[] _colliders;
        [FormerlySerializedAs("_drunkBuffDefinition")]
        [SerializeField]
        private BuffDefinition _buffDefinition;
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private string _fillAnimation;
        [SerializeField]
        private int _stack0Frame;
        [SerializeField]
        private int _stack1Frame;
        [SerializeField]
        private int _stack2Frame;
        [SerializeField]
        private int _stack3Frame;
        [SerializeField]
        private int _stack4Frame;
        [SerializeField]
        private int _stack5Frame;

        private bool _renderersResolved;
        private bool _collidersResolved;
        private bool _previewVisible;
        private Coroutine _fillAnimationRoutine;
        private RuntimeAnimatorController _cachedAnimatorController;
        private AnimationClip _cachedFillClip;

        public byte BeerStack => _beerStack;

        internal static byte GetBeerStack(NetworkString<_32> configurationHash)
        {
            return ParseBeerStack(configurationHash.ToString());
        }

        internal static NetworkString<_32> CreateConfigurationHash(byte beerStack)
        {
            string configuration = string.Concat(ConfigurationPrefix, beerStack.ToString(CultureInfo.InvariantCulture));

            NetworkString<_32> hash = default;
            hash = configuration;

            return hash;
        }

        private void Awake()
        {
            ResolveRenderers();
            ResolveColliders();
            SetVisualsVisible(false);
            SnapFillAnimation(GetSafeBeerStack());
        }

        private void OnEnable()
        {
            UpdateVisualState();
            SnapFillAnimation(GetSafeBeerStack());
        }

        private void OnDisable()
        {
            SetVisualsVisible(false);
            StopFillAnimationRoutine();
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
                    byte previousStack = _beerStack;
                    _beerStack--;

                    if (_beerStack != previousStack)
                    {
                        UpdateConfigurationHash();
                        HandleBeerStackChanged(previousStack, _beerStack, false);
                    }
                }

                Character character = Character;
                Agent agent = character != null ? character.Agent : null;

                BuffDefinition buffDefinition = _buffDefinition;
                if (buffDefinition != null)
                {
                    BuffSystem buffSystem = agent != null ? agent.BuffSystem : null;
                    buffSystem?.ApplyBuff(buffDefinition);
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
            byte previousStack = _beerStack;
            _beerStack = (byte)newValue;

            if (_beerStack != previousStack)
            {
                UpdateConfigurationHash();
                HandleBeerStackChanged(previousStack, _beerStack, false);
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

            byte previousStack = _beerStack;
            _beerStack = ParseBeerStack(configurationHash);
            HandleBeerStackChanged(previousStack, _beerStack, false);
        }

        private static byte ParseBeerStack(string configurationHash)
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

            NetworkString<_32> newHash = CreateConfigurationHash(_beerStack);

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
            NotifyDrinkFinished();
        }

        private byte GetSafeBeerStack()
        {
            NetworkObject networkObject = Object;

            if (networkObject == null || networkObject.IsValid == false)
            {
                return 0;
            }

            return _beerStack;
        }

        internal void SnapPreviewToStack(byte stack)
        {
            SnapFillAnimation(stack);
        }

        private void HandleBeerStackChanged(byte previousStack, byte newStack, bool immediate)
        {
            if (_animator == null || string.IsNullOrEmpty(_fillAnimation) == true)
            {
                return;
            }

            if (previousStack == newStack)
            {
                if (immediate == true)
                {
                    SnapFillAnimation(newStack);
                }

                return;
            }

            if (immediate == true || isActiveAndEnabled == false || gameObject.activeInHierarchy == false)
            {
                SnapFillAnimation(newStack);
                return;
            }

            PlayFillAnimation(previousStack, newStack);
        }

        private void PlayFillAnimation(byte previousStack, byte newStack)
        {
            if (_animator == null)
            {
                return;
            }

            AnimationClip clip = GetFillAnimationClip();

            if (clip == null)
            {
                SnapFillAnimation(newStack);
                return;
            }

            StopFillAnimationRoutine();
            _fillAnimationRoutine = StartCoroutine(AnimateFillAnimation(previousStack, newStack, clip));
        }

        private IEnumerator AnimateFillAnimation(byte previousStack, byte newStack, AnimationClip clip)
        {
            try
            {
                if (_animator == null || clip == null)
                {
                    yield break;
                }

                float totalFrames = clip.length * clip.frameRate;

                if (totalFrames <= 0f)
                {
                    SnapFillAnimation(newStack);
                    yield break;
                }

                float startNormalized = GetNormalizedFrame(previousStack, totalFrames);
                float endNormalized = GetNormalizedFrame(newStack, totalFrames);

                if (Mathf.Approximately(startNormalized, endNormalized) == true)
                {
                    SnapFillAnimation(newStack);
                    yield break;
                }

                float direction = Mathf.Sign(endNormalized - startNormalized);
                float clipLength = Mathf.Max(clip.length, Mathf.Epsilon);
                float current = startNormalized;

                _animator.speed = 0f;
                _animator.Play(_fillAnimation, 0, current);
                _animator.Update(0f);

                while ((direction > 0f && current < endNormalized) || (direction < 0f && current > endNormalized))
                {
                    yield return null;

                    float delta = (Time.deltaTime / clipLength) * direction;
                    current += delta;

                    if (direction > 0f)
                    {
                        current = Mathf.Min(current, endNormalized);
                    }
                    else
                    {
                        current = Mathf.Max(current, endNormalized);
                    }

                    _animator.Play(_fillAnimation, 0, current);
                    _animator.Update(0f);
                }

                _animator.speed = 0f;
            }
            finally
            {
                _fillAnimationRoutine = null;
            }
        }

        private void SnapFillAnimation(byte stack)
        {
            if (_animator == null || string.IsNullOrEmpty(_fillAnimation) == true)
            {
                return;
            }

            StopFillAnimationRoutine();

            AnimationClip clip = GetFillAnimationClip();

            if (clip == null)
            {
                return;
            }

            float totalFrames = clip.length * clip.frameRate;
            float normalizedTime = totalFrames > 0f ? GetNormalizedFrame(stack, totalFrames) : 0f;

            _animator.speed = 0f;
            _animator.Play(_fillAnimation, 0, normalizedTime);
            _animator.Update(0f);
        }

        private void StopFillAnimationRoutine()
        {
            if (_fillAnimationRoutine != null)
            {
                StopCoroutine(_fillAnimationRoutine);
                _fillAnimationRoutine = null;
            }

            if (_animator != null)
            {
                _animator.speed = 0f;
            }
        }

        private AnimationClip GetFillAnimationClip()
        {
            if (_animator == null)
            {
                return null;
            }

            RuntimeAnimatorController controller = _animator.runtimeAnimatorController;

            if (controller == null)
            {
                _cachedAnimatorController = null;
                _cachedFillClip = null;
                return null;
            }

            if (_cachedAnimatorController == controller && _cachedFillClip != null && _cachedFillClip.name == _fillAnimation)
            {
                return _cachedFillClip;
            }

            _cachedAnimatorController = controller;
            _cachedFillClip = null;

            if (string.IsNullOrEmpty(_fillAnimation) == true)
            {
                return null;
            }

            AnimationClip[] clips = controller.animationClips;

            if (clips == null)
            {
                return null;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null && clip.name == _fillAnimation)
                {
                    _cachedFillClip = clip;
                    break;
                }
            }

            return _cachedFillClip;
        }

        private float GetNormalizedFrame(int stack, float totalFrames)
        {
            int frame = GetFrameForStack(stack);
            return totalFrames > 0f ? Mathf.Clamp01(frame / totalFrames) : 0f;
        }

        private int GetFrameForStack(int stack)
        {
            int clamped = Mathf.Clamp(stack, 0, 5);

            switch (clamped)
            {
                case 0: return _stack0Frame;
                case 1: return _stack1Frame;
                case 2: return _stack2Frame;
                case 3: return _stack3Frame;
                case 4: return _stack4Frame;
                default: return _stack5Frame;
            }
        }
    }
}
