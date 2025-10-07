using TPSBR;
using TSS.Data;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class WeaponDefinition : ItemDefinition
    {
        [SerializeField]
        private Weapon _weaponPrefab;

        public Weapon WeaponPrefab
        {
            get
            {
                if (_weaponPrefab == null && NetworkPrefab != null)
                {
                    if (NetworkPrefab.TryGetComponent(out Weapon networkWeapon) == true)
                    {
                        _weaponPrefab = networkWeapon;
                    }
                }

                return _weaponPrefab;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_weaponPrefab == null && NetworkPrefab != null)
            {
                if (NetworkPrefab.TryGetComponent(out Weapon networkWeapon) == true)
                {
                    _weaponPrefab = networkWeapon;
                }
            }
        }
#endif
    }
}
