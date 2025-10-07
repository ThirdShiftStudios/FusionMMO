using TPSBR;
using TSS.Data;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class WeaponDefinition : ItemDefinition
    {
        [SerializeField]
        private Weapon _weaponPrefab;

        public Weapon WeaponPrefab => _weaponPrefab;
    }
}
