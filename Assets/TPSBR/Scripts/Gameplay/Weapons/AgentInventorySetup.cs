using UnityEngine;

namespace TPSBR
{
    [DisallowMultipleComponent]
    public sealed class AgentInventorySetup : MonoBehaviour
    {
        [SerializeField] private WeaponSlot[] _slots = System.Array.Empty<WeaponSlot>();
        [SerializeField] private WeaponSizeSlot[] _weaponSizeSlots = System.Array.Empty<WeaponSizeSlot>();
        [SerializeField] private Weapon[] _initialWeapons = System.Array.Empty<Weapon>();
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private InventoryItemPickupProvider _inventoryItemPickupPrefab;
        [SerializeField] private float _itemDropForwardOffset = 1.5f;
        [SerializeField] private float _itemDropUpOffset = 0.35f;
        [SerializeField] private float _itemDropImpulse = 3f;
        [SerializeField] private Transform _pickaxeEquippedParent;
        [SerializeField] private Transform _pickaxeUnequippedParent;
        [SerializeField] private Transform _woodAxeEquippedParent;
        [SerializeField] private Transform _woodAxeUnequippedParent;
        [SerializeField] private Transform _fireAudioEffectsRoot;

        public WeaponSlot[] Slots => _slots;
        public WeaponSizeSlot[] WeaponSizeSlots => _weaponSizeSlots;
        public Weapon[] InitialWeapons => _initialWeapons;
        public LayerMask HitMask => _hitMask;
        public InventoryItemPickupProvider InventoryItemPickupPrefab => _inventoryItemPickupPrefab;
        public float ItemDropForwardOffset => _itemDropForwardOffset;
        public float ItemDropUpOffset => _itemDropUpOffset;
        public float ItemDropImpulse => _itemDropImpulse;
        public Transform PickaxeEquippedParent => _pickaxeEquippedParent;
        public Transform PickaxeUnequippedParent => _pickaxeUnequippedParent;
        public Transform WoodAxeEquippedParent => _woodAxeEquippedParent;
        public Transform WoodAxeUnequippedParent => _woodAxeUnequippedParent;
        public Transform FireAudioEffectsRoot => _fireAudioEffectsRoot;
    }
}
