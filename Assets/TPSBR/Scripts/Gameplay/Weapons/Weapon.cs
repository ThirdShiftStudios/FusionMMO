using Fusion;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public enum WeaponUseAnimation
    {
        None,
        Charge,
        LightAttack,
        HeavyAttack,
        Ability,
        FishingCast,
        BeerDrink,
        CigaretteSmoke,
    }

    public readonly struct WeaponUseRequest
    {
        public static WeaponUseRequest None => default;

        public WeaponUseRequest(bool shouldUse, bool fireImmediately, WeaponUseAnimation animation, float chargeProgress = -1f)
        {
            ShouldUse = shouldUse;
            FireImmediately = fireImmediately;
            Animation = animation;
            ChargeProgress = chargeProgress;
        }

        public bool ShouldUse { get; }
        public bool FireImmediately { get; }
        public WeaponUseAnimation Animation { get; }
        public float ChargeProgress { get; }
        public bool HasChargeProgress => ChargeProgress >= 0f;

        public static WeaponUseRequest FireImmediate()
        {
            return new WeaponUseRequest(true, true, WeaponUseAnimation.None);
        }

        public static WeaponUseRequest CreateAnimation(WeaponUseAnimation animation, bool fireImmediately = false, float chargeProgress = -1f)
        {
            return new WeaponUseRequest(true, fireImmediately, animation, chargeProgress);
        }
    }

    public abstract class Weapon : ContextBehaviour, IInventoryItemDetails
    {
        // PUBLIC MEMBERS

        public int WeaponID => _weaponDefinition.ID;
        public WeaponDefinition Definition => _weaponDefinition;
        public Transform LeftHandTarget => _leftHandTarget;
        public EHitType HitType => _hitType;
        public float AimFOV => _aimFOV;
        public string DisplayName => _displayName;
        public string NameShortcut => _nameShortcut;
        public Sprite Icon => _weaponDefinition.Icon;
        public virtual string GetDescription()
        {
            return _description + " this is the description";
        }
        public virtual string GetDisplayName(NetworkString<_64> configurationHash)
        {
            return DisplayName;
        }
        public virtual string GetDescription(NetworkString<_64> configurationHash)
        {
            return GetDescription();
        }
        public bool ValidOnlyWithAmmo => _validOnlyWithAmmo;
        public bool IsInitialized => _isInitialized;
        public bool IsArmed => _isArmed;
        public NetworkObject Owner => _owner;
        public Character Character => _character;
        public WeaponSize Size => _weaponSize;
        public NetworkString<_64> ConfigurationHash => _configurationHash;

        // PRIVATE MEMBERS

        [SerializeField] private WeaponDefinition _weaponDefinition;
        [SerializeField] private WeaponSize _weaponSize = WeaponSize.Unarmed;
        [SerializeField] private bool _validOnlyWithAmmo;
        [SerializeField] private Transform _leftHandTarget;
        [SerializeField] private EHitType _hitType;
        [SerializeField] private float _aimFOV;

        [Header("Pickup")] [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [SerializeField, Tooltip("Up to 4 letter name shown in thumbnail")]
        private string _nameShortcut;

        [SerializeField] private Sprite _icon;

        //[Networked(OnChanged = nameof(OnConfigurationHashChanged))]
        [Networked]
        private NetworkString<_64> _configurationHash { get; set; }

        private bool _isInitialized;
        private bool _isArmed;
        private NetworkObject _owner;
        private Character _character;
        private Transform _armedParent;
        private Transform _disarmedParent;
        private AudioEffect[] _audioEffects;
        private NetworkString<_64> _appliedConfigurationHash;

        // PUBLIC METHODS

        public void ArmWeapon()
        {
            if (_isArmed == true)
                return;

            _isArmed = true;
            OnIsArmedChanged();
        }

        public void DisarmWeapon()
        {
            if (_isArmed == false)
                return;

            _isArmed = false;
            OnIsArmedChanged();
        }

        public void Initialize(NetworkObject owner, Transform armedParent, Transform disarmedParent)
        {
            if (_isInitialized == true)
            {
                if (_owner != owner)
                    return;

                bool parentsChanged = _armedParent != armedParent || _disarmedParent != disarmedParent;

                _armedParent = armedParent;
                _disarmedParent = disarmedParent;

                if (parentsChanged == true)
                {
                    RefreshParent();
                }

                return;
            }

            _isInitialized = true;
            _owner = owner;
            _character = owner.GetComponent<Character>();
            _armedParent = armedParent;
            _disarmedParent = disarmedParent;

            RefreshParent();
        }

        public void Deinitialize(NetworkObject owner)
        {
            if (_owner != null && _owner != owner)
                return;

            _isInitialized = default;
            _owner = default;
            _character = default;
            _armedParent = default;
            _disarmedParent = default;

            AssignFireAudioEffects(null, null);
        }

        public virtual bool IsBusy()
        {
            return false;
        }

        public abstract bool CanFire(bool keyDown);
        public abstract void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask);

        public virtual WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            if ((attackHeld == false && attackActivated == false) || CanFire(attackActivated) == false)
            {
                return WeaponUseRequest.None;
            }

            return WeaponUseRequest.FireImmediate();
        }

        public virtual void OnUseStarted(in WeaponUseRequest request)
        {
        }

        public virtual bool HandleAnimationRequest(UseLayer attackLayer, in WeaponUseRequest request)
        {
            return true;
        }

        public virtual bool CanReload(bool autoReload)
        {
            return false;
        }

        public virtual void Reload()
        {
        }

        public virtual bool CanAim()
        {
            return false;
        }

        public virtual void AssignFireAudioEffects(Transform root, AudioEffect[] audioEffects)
        {
            _audioEffects = audioEffects;
        }

        public virtual bool HasAmmo()
        {
            return true;
        }

        public virtual bool AddAmmo(int ammo)
        {
            return false;
        }

        public virtual bool CanFireToPosition(Vector3 firePosition, ref Vector3 targetPosition, LayerMask hitMask)
        {
            return true;
        }

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            ApplyConfigurationHash(_configurationHash);

            if (ApplicationSettings.IsStrippedBatch == true)
            {
                gameObject.SetActive(false);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (hasState == true)
            {
                DisarmWeapon();
            }
            else
            {
                _isArmed = false;
            }

            Deinitialize(_owner);
        }

        // PROTECTED METHODS

        protected virtual void OnWeaponArmed()
        {
        }

        protected virtual void OnWeaponDisarmed()
        {
        }

        protected bool PlaySound(AudioSetup setup)
        {
            if (_audioEffects.PlaySound(setup, EForceBehaviour.ForceAny) == false)
            {
                Debug.LogWarning(
                    $"No free audio effects on weapon {gameObject.name}. Add more audio effects in Player prefab.");
                return false;
            }

            return true;
        }

        // NETWORK CALLBACKS

        private void OnIsArmedChanged()
        {
            RefreshParent();

            if (IsArmed == true)
            {
                OnWeaponArmed();
            }
            else
            {
                OnWeaponDisarmed();
            }
        }

        private void RefreshParent()
        {
            if (_isInitialized == false)
                return;

            Transform targetParent = _isArmed == true ? _armedParent : _disarmedParent;
            if (targetParent == null)
                return;

            Vector3 worldScale = transform.lossyScale;

            transform.SetParent(targetParent, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            Vector3 parentScale = targetParent.lossyScale;
            transform.localScale = new Vector3(
                parentScale.x != 0f ? worldScale.x / parentScale.x : 0f,
                parentScale.y != 0f ? worldScale.y / parentScale.y : 0f,
                parentScale.z != 0f ? worldScale.z / parentScale.z : 0f);
        }
        

        private void ApplyConfigurationHash(NetworkString<_64> configurationHash)
        {
            if (_appliedConfigurationHash == configurationHash)
                return;

            _appliedConfigurationHash = configurationHash;
            OnConfigurationHashApplied(configurationHash.ToString());
        }

        protected virtual void OnConfigurationHashApplied(string configurationHash)
        {
        }

        protected void SetWeaponSize(WeaponSize size)
        {
            _weaponSize = size;
        }

        protected void SetDisplayName(string displayName)
        {
            _displayName = displayName;
        }

        protected void SetNameShortcut(string shortcut)
        {
            _nameShortcut = shortcut;
        }

        public virtual string GenerateRandomStats()
        {
            return string.Empty;
        }

        public void SetConfigurationHash(NetworkString<_64> configurationHash)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (_configurationHash == configurationHash)
                return;

            _configurationHash = configurationHash;
            ApplyConfigurationHash(_configurationHash);
        }
    }
}
