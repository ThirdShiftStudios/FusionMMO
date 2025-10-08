using Fusion;
using UnityEngine;
using TSS.Data;

namespace TPSBR
{
        public sealed class InventoryItemPickupProvider : NetworkBehaviour, IDynamicPickupProvider
        {
                [SerializeField]
                private Transform _interpolationTarget;
                [SerializeField]
                private Collider _collider;
                [SerializeField]
                private float _despawnTime = 60f;
                [SerializeField]
                private Vector3 _visualOffset;

                [Networked]
                public int ItemDefinitionId { get; private set; }
                [Networked]
                public byte Quantity { get; private set; }
                [Networked]
                public NetworkString<_32> ConfigurationHash { get; private set; }

                public ItemDefinition Definition => _definition;

                public string Name => _definition != null ? _definition.Name : string.Empty;
                public string Description
                {
                        get
                        {
                                if (_definition == null)
                                        return string.Empty;

                                return Quantity > 1 ? $"{Quantity}x {_definition.Name}" : _definition.Name;
                        }
                }

                public Transform InterpolationTarget => _interpolationTarget != null ? _interpolationTarget : transform;
                public Collider Collider => _collider;
                public float DespawnTime => _despawnTime;

                private ItemDefinition _definition;
                private GameObject _visualInstance;
                private bool _visualInitialized;

                public void Initialize(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash = default)
                {
                        if (HasStateAuthority == false)
                                return;

                        if (definition == null)
                                return;

                        ItemDefinitionId = definition.ID;
                        Quantity = quantity;
                        ConfigurationHash = configurationHash;
                        _definition = definition;
                        _visualInitialized = false;

                        RefreshVisual();
                }

                public void SetQuantity(byte quantity)
                {
                        if (HasStateAuthority == true)
                        {
                                Quantity = quantity;
                        }
                }

                public override void Spawned()
                {
                        base.Spawned();

                        if (_interpolationTarget == null)
                        {
                                _interpolationTarget = transform;
                        }

                        if (_collider == null)
                        {
                                _collider = GetComponent<Collider>();
                        }

                        if (_collider == null)
                        {
                                var sphere = gameObject.AddComponent<SphereCollider>();
                                sphere.isTrigger = true;
                                sphere.radius = 0.35f;
                                _collider = sphere;
                        }

                        if (_collider != null)
                        {
                                _collider.enabled = true;
                                _collider.isTrigger = true;
                                _collider.gameObject.layer = ObjectLayer.Interaction;
                        }

                        EnsureDefinition();
                        RefreshVisual();
                }

                public override void FixedUpdateNetwork()
                {
                        base.FixedUpdateNetwork();

                        if (_definition == null)
                        {
                                EnsureDefinition();
                                RefreshVisual();
                        }
                }

                public override void Render()
                {
                        if (_definition == null)
                        {
                                EnsureDefinition();
                                RefreshVisual();
                        }
                }

                public override void Despawned(NetworkRunner runner, bool hasState)
                {
                        base.Despawned(runner, hasState);
                        ClearVisual();
                        _definition = null;
                        _visualInitialized = false;
                }

                private void EnsureDefinition()
                {
                        if (_definition != null)
                                return;

                        if (ItemDefinitionId == 0)
                                return;

                        _definition = ItemDefinition.Get(ItemDefinitionId);
                }

                private void RefreshVisual()
                {
                        if (_visualInitialized == true)
                                return;

                        if (_definition == null)
                                return;

                        var graphic = _definition.WorldGraphic;
                        if (graphic == null)
                                return;

                        ClearVisual();

                        var parent = InterpolationTarget;
                        _visualInstance = Instantiate(graphic, parent);
                        _visualInstance.transform.localPosition += _visualOffset;
                        _visualInstance.transform.localRotation = Quaternion.identity;
                        _visualInstance.transform.localScale = Vector3.one;

                        var rigidbody = _visualInstance.GetComponent<Rigidbody>();
                        if (rigidbody != null)
                        {
                                Destroy(rigidbody);
                        }

                        _visualInitialized = true;
                }

                private void ClearVisual()
                {
                        if (_visualInstance != null)
                        {
                                Destroy(_visualInstance);
                                _visualInstance = null;
                        }

                        _visualInitialized = false;
                }

                // IInteraction INTERFACE

                string  IInteraction.Name        => Name;
                string  IInteraction.Description => Description;
                Vector3 IInteraction.HUDPosition => transform.position;
                bool    IInteraction.IsActive    => _collider != null && _collider.enabled;
        }
}
