using TPSBR;
using TSS.Data;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class WeaponDefinition : ItemDefinition
    {
        [SerializeField]
        private Weapon _weaponPrefab;
        [SerializeField, TextArea]
        private string _description;

        public Weapon WeaponPrefab => _weaponPrefab;
        public override ushort MaxStack => 1;
        public string Description => _description;
    }
}
