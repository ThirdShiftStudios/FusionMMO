using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public class UIWeapons : UIBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private CanvasGroup _weaponGroup;
		[SerializeField]
		private TextMeshProUGUI _magazineAmmo;
		[SerializeField]
		private TextMeshProUGUI _weaponAmmo;
		[SerializeField]
		private Image _weaponIcon;
		[SerializeField]
		private Image _weaponIconShadow;
		[SerializeField]
		private TextMeshProUGUI _weaponName;
		[SerializeField]
		private TextMeshProUGUI _weaponClass;
		[SerializeField]
		private Image _ammoProgress;
		[SerializeField]
		private string _reloadingText = "-";
		[SerializeField]
		private CanvasGroup _unarmedThumbnail;
		[SerializeField]
		private CanvasGroup _secondaryThumbnail;
		[SerializeField]
		private Image _secondaryThumbnailIcon;
		[SerializeField]
		private CanvasGroup _primaryThumbnail;
		[SerializeField]
		private Image _primaryThumbnailIcon;
		[SerializeField]
		private CanvasGroup _grenadesThumbnail;
		[SerializeField]
		private CanvasGroup _grenadeChangingGroup;
		[SerializeField]
		private float _thumbnailInactiveAlpha = 0.3f;
		[SerializeField]
		private Color _grenadeChangingColor = Color.yellow;
                [SerializeField]
                private Color _grenadeInactiveColor = Color.gray;
                [SerializeField]
                private Image[] _grenades;

                private static readonly WeaponSize[] SecondaryWeaponSizes = { WeaponSize.Staff, WeaponSize.Light };
                private static readonly WeaponSize[] PrimaryWeaponSizes = { WeaponSize.Heavy };

                private int _lastMagazineAmmo;
                private int _lastWeaponAmmo;
                private int _lastWeaponSlot;

		private NetworkId _lastPrimaryID;
		private NetworkId _lastSecondaryID;

		private Color _grenadeColor;

		// PUBLIC METHODS

		public void UpdateWeapons(Inventory inventory, AgentInput agentInput)
		{
			UpdateThumbnails(inventory, agentInput);

			var currentWeapon = inventory.CurrentWeapon as FirearmWeapon;
			if (currentWeapon == null)
			{
				_weaponGroup.SetVisibility(false);
				return;
			}

			_weaponGroup.SetVisibility(true);

			int currentMagazineAmmo = currentWeapon.IsReloading == false ? currentWeapon.MagazineAmmo : -1;

			if (currentMagazineAmmo != _lastMagazineAmmo)
			{
				_magazineAmmo.text = currentMagazineAmmo >= 0 ? currentMagazineAmmo.ToString() : _reloadingText;
				_lastMagazineAmmo = currentMagazineAmmo;

				if (currentMagazineAmmo >= 0)
				{
					float progress = currentMagazineAmmo / (float)currentWeapon.MaxMagazineAmmo;
					_ammoProgress.fillAmount = progress;
				}
			}

			if (currentWeapon.IsReloading == true)
			{
				float reloadProgress = 1f - currentWeapon.Cooldown / currentWeapon.ReloadTime;
				_ammoProgress.fillAmount = reloadProgress;
			}

			if (currentWeapon.WeaponAmmo != _lastWeaponAmmo)
			{
				_weaponAmmo.text = $"{currentWeapon.WeaponAmmo}";
				_lastWeaponAmmo = currentWeapon.WeaponAmmo;
			}

                        if (currentWeapon.GetInstanceID() != _lastWeaponSlot)
                        {
                                _weaponIcon.sprite = currentWeapon.Icon;
                                _weaponIconShadow.sprite = currentWeapon.Icon;
                                _weaponName.text = currentWeapon.DisplayName;
                                _weaponClass.text = GetClassText(currentWeapon.Size);

                                _lastWeaponSlot = currentWeapon.GetInstanceID();
                        }
                }

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_grenadeColor = _grenades[0].color;
		}

		// PRIVATE METHODS

                private void UpdateThumbnails(Inventory inventory, AgentInput agentInput)
                {
                        UpdateWeaponThumbnail(inventory, _secondaryThumbnail, _secondaryThumbnailIcon, SecondaryWeaponSizes, ref _lastSecondaryID);
                        UpdateWeaponThumbnail(inventory, _primaryThumbnail, _primaryThumbnailIcon, PrimaryWeaponSizes, ref _lastPrimaryID);

                        WeaponSize currentWeaponSize = inventory.CurrentWeaponSize;

                        _unarmedThumbnail.alpha   = currentWeaponSize == WeaponSize.Unarmed ? 1f : _thumbnailInactiveAlpha;
                        bool isSecondary = currentWeaponSize == WeaponSize.Light || currentWeaponSize == WeaponSize.Staff;
                        _secondaryThumbnail.alpha = isSecondary ? 1f : _thumbnailInactiveAlpha;
                        _primaryThumbnail.alpha   = currentWeaponSize == WeaponSize.Heavy ? 1f : _thumbnailInactiveAlpha;
                        _grenadesThumbnail.alpha  = currentWeaponSize == WeaponSize.Throwable ? 1f : _thumbnailInactiveAlpha;

                        _grenadeChangingGroup.SetVisibility(agentInput.IsCyclingGrenades);

                        int currentWeaponSlot = inventory.CurrentWeaponSlot;
                        int grenadeStartSlot = 5;
			bool hasAnyGrenade = false;
			var activeGrenadeColor = agentInput.IsCyclingGrenades == true ? _grenadeChangingColor : _grenadeColor;

			for (int i = 0; i < _grenades.Length; i++)
			{
				int grenadeSlot = grenadeStartSlot + i;
				bool hasGrenade = inventory.HasWeapon(grenadeSlot, true);

				if (hasGrenade == false)
				{
					_grenades[i].SetActive(false);
					continue;
				}

				var grenadeImage = _grenades[i];

				grenadeImage.SetActive(true);
				grenadeImage.color = currentWeaponSlot == grenadeSlot ? activeGrenadeColor : _grenadeInactiveColor;

				hasAnyGrenade |= hasGrenade;
			}

			_grenadesThumbnail.SetActive(hasAnyGrenade);
		}

                private void UpdateWeaponThumbnail(Inventory inventory, CanvasGroup thumbnail, Image weaponIcon, WeaponSize[] sizes, ref NetworkId lastWeaponID)
                {
                        Weapon weapon = null;

                        foreach (WeaponSize size in sizes)
                        {
                                var candidate = inventory.GetWeapon(size);
                                if (candidate != null && candidate.Object != null)
                                {
                                        weapon = candidate;
                                        break;
                                }
                        }

                        if (weapon == null || weapon.Object == null)
                        {
                                thumbnail.SetActive(false);
                                return;
                        }

                        thumbnail.SetActive(true);

                        if (lastWeaponID == weapon.Object.Id)
                                return;

                        weaponIcon.sprite = weapon.Icon;
                        lastWeaponID = weapon.Object.Id;
                }

                private string GetClassText(WeaponSize weaponSize)
                {
                        return weaponSize.ToDisplayClass();
                }
        }
}
