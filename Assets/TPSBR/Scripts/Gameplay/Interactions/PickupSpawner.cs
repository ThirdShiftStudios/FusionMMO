using System;
using Fusion;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using TSS.Data;
using Random = UnityEngine.Random;

namespace TPSBR
{
        public sealed class PickupSpawner : NetworkBehaviour
        {
                [SerializeField]
                private Transform _spawnPoint;
                [SerializeField]
                private InventoryItemPickupProvider _pickupPrefab;
                [SerializeField]
                private ItemDefinition[] _possibleItems;
                [SerializeField]
                private float _refillTime = 30;

                [Networked]
                private TickTimer _refillCooldown { get; set; }
                [Networked]
                private InventoryItemPickupProvider _activePickup { get; set; }

                // NetworkBehaviour INTERFACE

                public override void FixedUpdateNetwork()
                {
			if (HasStateAuthority == false)
				return;

                        if (_activePickup != null)
                        {
                                if (_activePickup.Object.IsValid == true && _activePickup.Quantity > 0)
                                        return;

                                _activePickup = null;
                                _refillCooldown = TickTimer.CreateFromSeconds(Runner, _refillTime);
                                return;
                        }

                        if (_refillCooldown.ExpiredOrNotRunning(Runner) == false)
                                return;

                        if (_pickupPrefab == null)
                                return;

                        if (_possibleItems == null || _possibleItems.Length == 0)
                                return;

                        try
                        {
                                var definition = _possibleItems[Random.Range(0, _possibleItems.Length)];
                                if (definition == null)
                                        return;

                                Vector3 position = _spawnPoint != null ? _spawnPoint.position : transform.position;
                                Quaternion rotation = _spawnPoint != null ? _spawnPoint.rotation : transform.rotation;

                                _activePickup = Runner.Spawn(_pickupPrefab, position, rotation);

                                NetworkString<_32> configurationHash = default;

                                if (definition is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null)
                                {
                                        string randomStats = weaponDefinition.WeaponPrefab.GenerateRandomStats();
                                        if (string.IsNullOrWhiteSpace(randomStats) == false)
                                        {
                                                configurationHash = randomStats;
                                        }
                                }
                                else if (definition is PickaxeDefinition pickaxeDefinition && pickaxeDefinition.PickaxePrefab != null)
                                {
                                        string randomStats = pickaxeDefinition.PickaxePrefab.GenerateRandomStats();
                                        if (string.IsNullOrWhiteSpace(randomStats) == false)
                                        {
                                                configurationHash = randomStats;
                                        }
                                }
                                else if (definition is WoodAxeDefinition woodAxeDefinition && woodAxeDefinition.WoodAxePrefab != null)
                                {
                                        string randomStats = woodAxeDefinition.WoodAxePrefab.GenerateRandomStats();
                                        if (string.IsNullOrWhiteSpace(randomStats) == false)
                                        {
                                                configurationHash = randomStats;
                                        }
                                }

                                _activePickup.Initialize(definition, 1, configurationHash);
                        }
                        catch (Exception e)
                        {
                                Debug.LogError($"{gameObject.name} nono");
                                throw;
			}
		}
	}
}
