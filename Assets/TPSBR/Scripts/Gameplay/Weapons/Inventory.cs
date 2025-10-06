namespace TPSBR
{
	using System;
        using UnityEngine;
        using Fusion;
        using TSS.Data;

	[Serializable]
	public sealed class WeaponSlot
	{
		public Transform  Active;
		public Transform  Inactive;
		[NonSerialized]
		public Quaternion BaseRotation;
	}

        public struct InventorySlot : INetworkStruct, IEquatable<InventorySlot>
        {
                public InventorySlot(int itemDefinitionId, byte quantity, NetworkString<_32> configurationHash)
                {
                        ItemDefinitionId = itemDefinitionId;
                        Quantity = quantity;
                        ConfigurationHash = configurationHash;
                }

                public int ItemDefinitionId { get; private set; }
                public byte Quantity { get; private set; }
                public NetworkString<_32> ConfigurationHash { get; private set; }

                public bool IsEmpty => Quantity == 0;

                public void Clear()
                {
                        ItemDefinitionId = 0;
                        Quantity = 0;
                        ConfigurationHash = default;
                }

                public void Add(byte amount)
                {
                        int newQuantity = Quantity + amount;
                        Quantity = (byte)Mathf.Clamp(newQuantity, 0, byte.MaxValue);
                }

                public void Remove(byte amount)
                {
                        int newQuantity = Quantity - amount;
                        Quantity = (byte)Mathf.Clamp(newQuantity, 0, byte.MaxValue);

                        if (Quantity == 0)
                        {
                                ItemDefinitionId = 0;
                                ConfigurationHash = default;
                        }
                }

                public bool Equals(InventorySlot other)
                {
                        return ItemDefinitionId == other.ItemDefinitionId && Quantity == other.Quantity && ConfigurationHash == other.ConfigurationHash;
                }

                public override bool Equals(object obj)
                {
                        return obj is InventorySlot other && Equals(other);
                }

                public override int GetHashCode()
                {
                        unchecked
                        {
                                int hashCode = ItemDefinitionId;
                                hashCode = (hashCode * 397) ^ Quantity.GetHashCode();
                                hashCode = (hashCode * 397) ^ ConfigurationHash.GetHashCode();
                                return hashCode;
                        }
                }

                public ItemDefinition GetDefinition()
                {
                        return Quantity == 0 ? null : ItemDefinition.Get(ItemDefinitionId);
                }
        }
	public sealed class Inventory : NetworkBehaviour, IBeforeTick
	{
		// PUBLIC MEMBERS
		public Weapon     CurrentWeapon             { get; private set; }
		public Transform  CurrentWeaponHandle       { get; private set; }
		public Quaternion CurrentWeaponBaseRotation { get; private set; }

		public LayerMask  HitMask            => _hitMask;
		public int        CurrentWeaponSlot  => _currentWeaponSlot;
		public int        PreviousWeaponSlot => _previousWeaponSlot;
		public const int INVENTORY_SIZE = 10;
		// PRIVATE MEMBERS

		[SerializeField]
		private WeaponSlot[] _slots;
		[SerializeField]
		private Weapon[]     _initialWeapons;
		[SerializeField]
		private Vector3      _dropWeaponImpulse = new Vector3(5, 5f, 10f);
		[SerializeField]
		private LayerMask    _hitMask;

		[Header("Audio")]
		[SerializeField]
		private Transform    _fireAudioEffectsRoot;

                [Networked, Capacity(8)]
                private NetworkArray<Weapon> _hotbar { get; }
                [Networked, Capacity(INVENTORY_SIZE)]
                private NetworkArray<InventorySlot> _items { get; }
		[Networked]
		private byte _currentWeaponSlot { get; set; }

		[Networked]
		private byte _previousWeaponSlot { get; set; }

		private Health        _health;
		private Character     _character;
		private Interactions  _interactions;
                private AudioEffect[]   _fireAudioEffects;
                private Weapon[]        _localWeapons = new Weapon[8];
                private InventorySlot[] _localItems;

                public event Action<int, InventorySlot> ItemSlotChanged;

                // PUBLIC METHODS

                public int InventorySize => _items.Length;

                public InventorySlot GetItemSlot(int index)
                {
                        if (index < 0 || index >= _items.Length)
                                return default;

                        return _items[index];
                }

                public byte AddItem(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash = default)
                {
                        if (definition == null || quantity == 0)
                                return quantity;

                        if (HasStateAuthority == false)
                                return quantity;

                        return AddItemInternal(definition, quantity, configurationHash);
                }

                public void RequestMoveItem(int fromIndex, int toIndex)
                {
                        if (fromIndex == toIndex)
                                return;

                        if (fromIndex < 0 || fromIndex >= _items.Length)
                                return;

                        if (toIndex < 0 || toIndex >= _items.Length)
                                return;

                        if (HasStateAuthority == true)
                        {
                                MoveItem((byte)fromIndex, (byte)toIndex);
                        }
                        else
                        {
                                RPC_RequestMoveItem((byte)fromIndex, (byte)toIndex);
                        }
                }

                public void DisarmCurrentWeapon()
		{
			if (_currentWeaponSlot == 0)
				return;

			if (CurrentWeapon != null)
			{
				CurrentWeapon.DisarmWeapon();
			}

			if (_currentWeaponSlot > 0)
			{
				_previousWeaponSlot = _currentWeaponSlot;
			}

			_currentWeaponSlot = 0;

			CurrentWeapon             = _hotbar[_currentWeaponSlot];
			CurrentWeaponHandle       = _slots[_currentWeaponSlot].Active;
			CurrentWeaponBaseRotation = _slots[_currentWeaponSlot].BaseRotation;

			if (CurrentWeapon != null)
			{
				CurrentWeapon.ArmWeapon();
			}
		}

		public void SetCurrentWeapon(int slot)
		{
			if (_currentWeaponSlot == slot)
				return;

			_currentWeaponSlot = (byte)slot;
			CurrentWeapon = _hotbar[_currentWeaponSlot];
		}

		public void ArmCurrentWeapon()
		{
			if (CurrentWeapon != null)
			{
				CurrentWeapon.DisarmWeapon();
			}

			if (_currentWeaponSlot > 0)
			{
				_previousWeaponSlot = _currentWeaponSlot;
			}

			CurrentWeapon             = _hotbar[_currentWeaponSlot];
			CurrentWeaponHandle       = _slots[_currentWeaponSlot].Active;
			CurrentWeaponBaseRotation = _slots[_currentWeaponSlot].BaseRotation;

			if (CurrentWeapon != null)
			{
				CurrentWeapon.ArmWeapon();
			}
		}

		public void DropCurrentWeapon()
		{
			DropWeapon(_currentWeaponSlot);
		}

                public void Pickup(DynamicPickup dynamicPickup, Weapon pickupWeapon)
                {
                        if (HasStateAuthority == false)
                                return;

			var ownedWeapon = _hotbar[pickupWeapon.WeaponSlot];
			if (ownedWeapon != null && ownedWeapon.WeaponID == pickupWeapon.WeaponID)
			{
				// We already have this weapon, try add at least the ammo
				var firearmWeapon = pickupWeapon as FirearmWeapon;
				bool consumed = firearmWeapon != null && ownedWeapon.AddAmmo(firearmWeapon.TotalAmmo);

				if (consumed == true)
				{
					dynamicPickup.UnassignObject();
					Runner.Despawn(pickupWeapon.Object);
				}
			}
			else
			{
				dynamicPickup.UnassignObject();
                                PickupWeapon(pickupWeapon);
                        }
                }

                public void Pickup(DynamicPickup dynamicPickup, InventoryItemPickupProvider provider)
                {
                        if (HasStateAuthority == false || provider == null)
                                return;

                        var definition = provider.Definition;
                        if (definition == null)
                                return;

                        byte quantity = provider.Quantity;
                        if (quantity == 0)
                                return;

                        byte remainder = AddItemInternal(definition, quantity, provider.ConfigurationHash);

                        if (remainder == quantity)
                                return;

                        provider.SetQuantity(remainder);

                        if (remainder == 0)
                        {
                                dynamicPickup.UnassignObject();
                                Runner.Despawn(provider.Object);
                        }
                }

		public void Pickup(WeaponPickup weaponPickup)
		{
			if (HasStateAuthority == false)
				return;

			if (weaponPickup.Consumed == true || weaponPickup.IsDisabled == true)
				return;

			var ownedWeapon = _hotbar[weaponPickup.WeaponPrefab.WeaponSlot];
			if (ownedWeapon != null && ownedWeapon.WeaponID == weaponPickup.WeaponPrefab.WeaponID)
			{
				// We already have this weapon, try add at least the ammo
				var firearmWeapon = weaponPickup.WeaponPrefab as FirearmWeapon;
				bool consumed = firearmWeapon != null && ownedWeapon.AddAmmo(firearmWeapon.InitialAmmo);

				if (consumed == true)
				{
					weaponPickup.TryConsume(gameObject, out string weaponPickupResult);
				}
			}
			else
			{
				weaponPickup.TryConsume(gameObject, out string weaponPickupResult2);

				var weapon = Runner.Spawn(weaponPickup.WeaponPrefab, inputAuthority: Object.InputAuthority);
				PickupWeapon(weapon);
			}
		}

                public override void Spawned()
                {
                        if (HasStateAuthority == false)
                        {
                                RefreshWeapons();
                                RefreshItems();
                                return;
                        }

			_currentWeaponSlot  = 0;
			_previousWeaponSlot = 0;

			byte bestWeaponSlot = 0;

			// Spawn initial weapons
			for (byte i = 0; i < _initialWeapons.Length; i++)
			{
				var weaponPrefab = _initialWeapons[i];
				if (weaponPrefab == null)
					continue;

				var weapon = Runner.Spawn(weaponPrefab, inputAuthority: Object.InputAuthority);
				AddWeapon(weapon);

				if (weapon.WeaponSlot > bestWeaponSlot && weapon.WeaponSlot < 3)
				{
					bestWeaponSlot = (byte)weapon.WeaponSlot;
				}
			}

			_previousWeaponSlot = bestWeaponSlot;

                        SetCurrentWeapon(bestWeaponSlot);
                        ArmCurrentWeapon();
                        RefreshWeapons();
                        RefreshItems();
                }

		public void OnDespawned()
		{
			// Cleanup weapons
			for (int i = 0; i < _hotbar.Length; i++)
			{
				Weapon weapon = _hotbar[i];
				if (weapon != null)
				{
					weapon.Deinitialize(Object);
					Runner.Despawn(weapon.Object);
					_hotbar.Set(i, null);
					_localWeapons[i] = null;
				}
			}

			for (int i = 0; i < _localWeapons.Length; i++)
			{
				Weapon weapon = _localWeapons[i];
				if (weapon != null)
				{
					weapon.Deinitialize(Object);
					_localWeapons[i] = null;
				}
			}

                        _currentWeaponSlot  = 0;
                        _previousWeaponSlot = 0;

                        CurrentWeapon             = default;
                        CurrentWeaponHandle       = default;
                        CurrentWeaponBaseRotation = default;

                        if (_localItems != null)
                        {
                                Array.Clear(_localItems, 0, _localItems.Length);
                        }

                        ItemSlotChanged = null;
                }

		public void OnFixedUpdate()
		{
			if (HasStateAuthority == false)
				return;

			if (_health.IsAlive == false)
			{
				DropAllWeapons();
				return;
			}

			// Autoswitch to valid weapon if current is invalid
			if (CurrentWeapon != null && CurrentWeapon.ValidOnlyWithAmmo == true && CurrentWeapon.HasAmmo() == false)
			{
				byte bestWeaponSlot = _previousWeaponSlot;
				if (bestWeaponSlot == 0 || bestWeaponSlot == _currentWeaponSlot)
				{
					bestWeaponSlot = FindBestWeaponSlot(_currentWeaponSlot);
				}

				DisarmCurrentWeapon();
				SetCurrentWeapon(bestWeaponSlot);

				_previousWeaponSlot = bestWeaponSlot;
			}
		}

        public override void Render()
        {
                RefreshWeapons();
                RefreshItems();
        }

		public bool CanFireWeapon(bool keyDown)
		{
			return CurrentWeapon != null && CurrentWeapon.CanFire(keyDown) == true;
		}

		public bool CanReloadWeapon(bool autoReload)
		{
			return CurrentWeapon != null && CurrentWeapon.CanReload(autoReload) == true;
		}

		public bool CanAim()
		{
			return CurrentWeapon != null && CurrentWeapon.CanAim() == true;
		}

		public Vector2 GetRecoil()
		{
			var firearmWeapon = CurrentWeapon as FirearmWeapon;
			var recoil = firearmWeapon != null ? firearmWeapon.Recoil : Vector2.zero;
			return new Vector2(-recoil.y, recoil.x); // Convert to axis angles
		}

		public bool HasWeapon(int slot, bool checkAmmo = false)
		{
			if (slot < 0 || slot >= _hotbar.Length)
				return false;

			var weapon = _hotbar[slot];
			return weapon != null && (checkAmmo == false || (weapon.Object != null && weapon.HasAmmo() == true));
		}

		public Weapon GetWeapon(int slot)
		{
			return _hotbar[slot];
		}

		public int GetNextWeaponSlot(int fromSlot, int minSlot = 0, bool checkAmmo = true)
		{
			int weaponCount = _hotbar.Length;

			for (int i = 0; i < weaponCount; i++)
			{
				int slot = (i + fromSlot + 1) % weaponCount;

				if (slot < minSlot)
					continue;

				var weapon = _hotbar[slot];

				if (weapon == null)
					continue;

				if (checkAmmo == true && weapon.HasAmmo() == false)
					continue;

				return slot;
			}

			return 0;
		}

		public bool Fire()
		{
			if (CurrentWeapon == null)
				return false;

			Vector3       targetPoint   = _interactions.GetTargetPoint(false, true);
			TransformData fireTransform = _character.GetFireTransform(true);

			CurrentWeapon.Fire(fireTransform.Position, targetPoint, _hitMask);

			return true;
		}

		public bool Reload()
		{
			if (CurrentWeapon == null)
				return false;

			CurrentWeapon.Reload();
			return true;
		}

		public bool AddAmmo(int weaponSlot, int amount, out string result)
		{
			if (weaponSlot < 0 || weaponSlot >= _hotbar.Length)
			{
				result = string.Empty;
				return false;
			}

			var weapon = _hotbar[weaponSlot];
			if (weapon == null)
			{
				result = "No weapon with this type of ammo";
				return false;
			}

			bool ammoAdded = weapon.AddAmmo(amount);
			result = ammoAdded == true ? string.Empty : "Cannot add more ammo";

			return ammoAdded;
		}

		// IBeforeTick INTERFACE

        void IBeforeTick.BeforeTick()
        {
                RefreshWeapons();
                RefreshItems();
        }

		// MONOBEHAVIOUR

        private void Awake()
        {
                _health = GetComponent<Health>();
                _character = GetComponent<Character>();
                _interactions = GetComponent<Interactions>();
                _fireAudioEffects = _fireAudioEffectsRoot.GetComponentsInChildren<AudioEffect>();
                _localItems = new InventorySlot[INVENTORY_SIZE];

			foreach (WeaponSlot slot in _slots)
			{
				if (slot.Active != null)
				{
					slot.BaseRotation = slot.Active.localRotation;
				}
			}
		}

		// PRIVATE METHODS

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestMoveItem(byte fromIndex, byte toIndex)
        {
                MoveItem(fromIndex, toIndex);
        }

        private byte AddItemInternal(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash)
        {
                ushort maxStack = ItemDefinition.GetMaxStack(definition.ID);
                if (maxStack == 0)
                {
                        maxStack = 1;
                }

                int clampedMaxStack = Mathf.Clamp(maxStack, 1, byte.MaxValue);
                byte maxStackByte = (byte)clampedMaxStack;

                byte remaining = quantity;

                for (int i = 0; i < _items.Length && remaining > 0; i++)
                {
                        var slot = _items[i];
                        if (slot.IsEmpty == true)
                                continue;

                        if (slot.ItemDefinitionId != definition.ID)
                                continue;

                        if (slot.ConfigurationHash != configurationHash)
                                continue;

                        if (slot.Quantity >= maxStackByte)
                                continue;

                        byte space = (byte)Mathf.Min(maxStackByte - slot.Quantity, remaining);
                        if (space == 0)
                                continue;

                        slot.Add(space);
                        _items.Set(i, slot);
                        remaining -= space;
                }

                for (int i = 0; i < _items.Length && remaining > 0; i++)
                {
                        var slot = _items[i];
                        if (slot.IsEmpty == false)
                                continue;

                        byte addAmount = (byte)Mathf.Min(maxStackByte, remaining);
                        slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                        _items.Set(i, slot);
                        remaining -= addAmount;
                }

                if (remaining != quantity)
                {
                        RefreshItems();
                }

                return remaining;
        }

        private void MoveItem(byte fromIndex, byte toIndex)
        {
                if (fromIndex == toIndex)
                        return;

                var fromSlot = _items[fromIndex];
                if (fromSlot.IsEmpty == true)
                        return;

                var toSlot = _items[toIndex];

                if (toSlot.IsEmpty == true)
                {
                        _items.Set(toIndex, fromSlot);
                        _items.Set(fromIndex, default);
                        RefreshItems();
                        return;
                }

                if (fromSlot.ItemDefinitionId == toSlot.ItemDefinitionId && fromSlot.ConfigurationHash == toSlot.ConfigurationHash)
                {
                        ushort maxStack = ItemDefinition.GetMaxStack(fromSlot.ItemDefinitionId);
                        if (maxStack == 0)
                        {
                                maxStack = 1;
                        }

                        int clampedMaxStack = Mathf.Clamp(maxStack, 1, byte.MaxValue);
                        if (toSlot.Quantity < clampedMaxStack)
                        {
                                byte space = (byte)Mathf.Min(clampedMaxStack - toSlot.Quantity, fromSlot.Quantity);
                                if (space > 0)
                                {
                                        toSlot.Add(space);
                                        fromSlot.Remove(space);

                                        _items.Set(toIndex, toSlot);

                                        if (fromSlot.IsEmpty == true)
                                        {
                                                _items.Set(fromIndex, default);
                                        }
                                        else
                                        {
                                                _items.Set(fromIndex, fromSlot);
                                        }

                                        RefreshItems();
                                        return;
                                }
                        }
                }

                _items.Set(toIndex, fromSlot);
                _items.Set(fromIndex, toSlot);
                RefreshItems();
        }

        private void RefreshItems()
        {
                int length = _items.Length;
                if (_localItems == null || _localItems.Length != length)
                {
                        _localItems = new InventorySlot[length];
                }

                for (int i = 0; i < length; i++)
                {
                        var slot = _items[i];
                        if (_localItems[i].Equals(slot) == false)
                        {
                                _localItems[i] = slot;
                                ItemSlotChanged?.Invoke(i, slot);
                        }
                }
        }

        private void RefreshWeapons()
		{
			// keep previous reference BEFORE reading the networked value
			var previousWeapon = CurrentWeapon;
			var nextWeapon     = _hotbar[_currentWeaponSlot];

			Vector2 lastRecoil = Vector2.zero;

			// Initialize and keep last recoil from armed weapons
			for (int i = 0; i < _hotbar.Length; i++)
			{
				var weapon = _hotbar[i];
				if (weapon == null)
					continue;

				if (weapon.IsInitialized == false)
				{
					weapon.Initialize(Object, _slots[weapon.WeaponSlot].Active, _slots[weapon.WeaponSlot].Inactive);
					weapon.AssignFireAudioEffects(_fireAudioEffectsRoot, _fireAudioEffects);
					_localWeapons[weapon.WeaponSlot] = weapon;
				}

				// Disarm non-current armed weapons
				if (weapon.IsArmed == true && weapon.WeaponSlot != _currentWeaponSlot)
				{
					weapon.DisarmWeapon();
				}

				if (weapon.IsArmed == true && weapon is FirearmWeapon fw)
				{
					lastRecoil = fw.Recoil;
				}
			}

			// Only run swap logic when the slot changed (or weapon ref changed)
			if (previousWeapon != nextWeapon)
			{
				// Disarm previously current weapon
				if (previousWeapon != null && previousWeapon.IsArmed)
				{
					previousWeapon.DisarmWeapon();
				}

				CurrentWeapon             = nextWeapon;
				CurrentWeaponHandle       = _slots[_currentWeaponSlot].Active;
				CurrentWeaponBaseRotation = _slots[_currentWeaponSlot].BaseRotation;

				if (CurrentWeapon != null)
				{
					CurrentWeapon.ArmWeapon();

					if (CurrentWeapon is FirearmWeapon newFw)
					{
						// transfer recoil
						newFw.Recoil = lastRecoil;
					}
				}
				else
				{
					// make sure local cache clears when weapon is gone
					_localWeapons[_currentWeaponSlot] = default;
				}
			}
		}


		private void DropAllWeapons()
		{
			for (int i = 1; i < _hotbar.Length; i++)
			{
				DropWeapon(i);
			}
		}

		private void DropWeapon(int weaponSlot)
		{
			var weapon = _hotbar[weaponSlot];
			if (weapon == null)
				return;

			if (weapon.PickupPrefab == null)
			{
				Debug.LogWarning($"Cannot drop weapon {gameObject.name}, pickup prefab not assigned.");
				return;
			}

			weapon.Deinitialize(Object);

			if (weaponSlot == _currentWeaponSlot)
			{
				byte bestWeaponSlot = _previousWeaponSlot;
				if (bestWeaponSlot == 0 || bestWeaponSlot == _currentWeaponSlot)
				{
					bestWeaponSlot = FindBestWeaponSlot(_currentWeaponSlot);
				}

				SetCurrentWeapon(bestWeaponSlot);
				ArmCurrentWeapon();

				_previousWeaponSlot = bestWeaponSlot;
			}

			var weaponTransform = weapon.transform;

			var pickup = Runner.Spawn(weapon.PickupPrefab, weaponTransform.position, weaponTransform.rotation,
				PlayerRef.None, BeforePickupSpawned);

			RemoveWeapon(weaponSlot);

			var pickupRigidbody = pickup.GetComponent<Rigidbody>();
			if (pickupRigidbody != null)
			{
				var forcePosition = weaponTransform.TransformPoint(new Vector3(-0.005f, 0.005f, 0.015f) * weaponSlot);
				pickupRigidbody.AddForceAtPosition(weaponTransform.rotation * _dropWeaponImpulse, forcePosition, ForceMode.Impulse);
			}

			void BeforePickupSpawned(NetworkRunner runner, NetworkObject obj)
			{
				var dynamicPickup = obj.GetComponent<DynamicPickup>();
				dynamicPickup.AssignObject(_hotbar[weaponSlot].Object.Id);
			}
		}

		private void PickupWeapon(Weapon weapon)
		{
			if (weapon == null)
				return;

			DropWeapon(weapon.WeaponSlot);
			AddWeapon(weapon);

			if (weapon.WeaponSlot >= _currentWeaponSlot && weapon.WeaponSlot < 5)
			{
				SetCurrentWeapon(weapon.WeaponSlot);
				ArmCurrentWeapon();
			}
		}

		private void AddWeapon(Weapon weapon)
		{
			if (weapon == null)
				return;

			RemoveWeapon(weapon.WeaponSlot);

			weapon.Object.AssignInputAuthority(Object.InputAuthority);
			weapon.Initialize(Object, _slots[weapon.WeaponSlot].Active, _slots[weapon.WeaponSlot].Inactive);
			weapon.AssignFireAudioEffects(_fireAudioEffectsRoot, _fireAudioEffects);

			var aoiProxy = weapon.GetComponent<NetworkAreaOfInterestProxy>();
			aoiProxy.SetPositionSource(transform);

			Runner.SetPlayerAlwaysInterested(Object.InputAuthority, weapon.Object, true);

			_hotbar.Set(weapon.WeaponSlot, weapon);
			_localWeapons[weapon.WeaponSlot] = weapon;
		}

		private void RemoveWeapon(int slot)
		{
			var weapon = _hotbar[slot];
			if (weapon == null)
				return;

			weapon.Deinitialize(Object);
			weapon.Object.RemoveInputAuthority();

			var aoiProxy = weapon.GetComponent<NetworkAreaOfInterestProxy>();
			aoiProxy.ResetPositionSource();

			Runner.SetPlayerAlwaysInterested(Object.InputAuthority, weapon.Object, false);

			_hotbar.Set(slot, null);
			_localWeapons[slot] = null;
		}

		private byte FindBestWeaponSlot(int ignoreSlot)
		{
			byte bestWeaponSlot = 0;

			for (int i = 0; i < _hotbar.Length; i++)
			{
				Weapon weapon = _hotbar[i];
				if (weapon != null)
				{
					if (weapon.WeaponSlot == ignoreSlot)
						continue;

					if (weapon.WeaponSlot > bestWeaponSlot && weapon.WeaponSlot < 3)
					{
						bestWeaponSlot = (byte)weapon.WeaponSlot;
					}
				}
			}

			return bestWeaponSlot;
		}

		public void SwitchWeapon(int inputWeapon)
		{
			if(_currentWeaponSlot == inputWeapon)
				return;
			SetCurrentWeapon(inputWeapon);
			ArmCurrentWeapon();
		}
	}
}
