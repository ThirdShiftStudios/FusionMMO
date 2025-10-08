using Fusion;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using TSS.Data;

namespace TPSBR
{
        public sealed class InventoryItemSpawner : NetworkBehaviour
        {
                [SerializeField]
                private InventoryItemPickupProvider _itemPrefab;
                [SerializeField]
                private ItemDefinition[] _possibleItems;
                [SerializeField]
                private Vector2Int _spawnCountRange = new Vector2Int(1, 3);
                [SerializeField]
                private Vector2Int _quantityRange = new Vector2Int(1, 1);
                [SerializeField]
                private Vector3 _spawnAreaExtents = new Vector3(2f, 0.5f, 2f);
                [SerializeField]
                private float _spawnHeightOffset = 0.5f;

                [Networked]
                private NetworkBool _hasSpawned { get; set; }

                public override void Spawned()
                {
                        if (HasStateAuthority == true && _hasSpawned == false)
                        {
                                SpawnItems();
                                _hasSpawned = true;
                        }
                }

                public void Respawn()
                {
                        if (HasStateAuthority == false)
                                return;

                        SpawnItems();
                }

                private void SpawnItems()
                {
                        if (_itemPrefab == null)
                                return;

                        if (_possibleItems == null || _possibleItems.Length == 0)
                                return;

                        int minCount = Mathf.Max(0, _spawnCountRange.x);
                        int maxCount = Mathf.Max(minCount, _spawnCountRange.y);
                        int spawnCount = Random.Range(minCount, maxCount + 1);

                        for (int i = 0; i < spawnCount; i++)
                        {
                                var definition = _possibleItems[Random.Range(0, _possibleItems.Length)];
                                if (definition == null)
                                        continue;

                                int minQuantity = Mathf.Max(1, _quantityRange.x);
                                int maxQuantity = Mathf.Max(minQuantity, _quantityRange.y);
                                byte quantity = (byte)Mathf.Clamp(Random.Range(minQuantity, maxQuantity + 1), 1, byte.MaxValue);

                                Vector3 randomOffset = new Vector3(
                                        Random.Range(-_spawnAreaExtents.x, _spawnAreaExtents.x),
                                        Random.Range(-_spawnAreaExtents.y, _spawnAreaExtents.y),
                                        Random.Range(-_spawnAreaExtents.z, _spawnAreaExtents.z));

                                Vector3 spawnPosition = transform.position + randomOffset;
                                spawnPosition.y += _spawnHeightOffset;

                                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                                var provider = Runner.Spawn(_itemPrefab, spawnPosition, rotation);
                                NetworkString<_32> configurationHash = default;

                                if (definition is WeaponDefinition weaponDefinition &&
                                    weaponDefinition.WeaponPrefab != null)
                                {
                                        string randomStats = weaponDefinition.WeaponPrefab.GenerateRandomStats();
                                        if (string.IsNullOrWhiteSpace(randomStats) == false)
                                        {
                                                configurationHash = randomStats;
                                        }
                                }

                                provider.Initialize(definition, quantity, configurationHash);
                        }
                }
        }
}
