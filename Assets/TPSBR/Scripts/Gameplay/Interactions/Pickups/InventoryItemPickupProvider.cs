using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;
using TSS.Data;

namespace TPSBR
{
        [RequireComponent(typeof(NetworkRigidbody3D))]
        [RequireComponent(typeof(Rigidbody))]
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
                [SerializeField]
                private ItemDefinitionIconDisplay _iconDisplay;
                [SerializeField]
                private NetworkRigidbody3D _networkRigidbody;
                [SerializeField]
                private Rigidbody _rigidbody;

                [Networked]
                public int ItemDefinitionId { get; private set; }
                [Networked]
                public byte Quantity { get; private set; }
                [Networked]
                public NetworkString<_32> ConfigurationHash { get; private set; }
                [Networked]
                private TickTimer _despawnTimer { get; set; }

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

                        if (_despawnTime > 0f)
                        {
                                _despawnTimer = TickTimer.CreateFromSeconds(Runner, _despawnTime);
                        }
                        else
                        {
                                _despawnTimer = default;
                        }

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

                        if (_iconDisplay == null)
                        {
                                _iconDisplay = GetComponent<ItemDefinitionIconDisplay>();
                        }

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
                                sphere.radius = 0.35f;
                                _collider = sphere;
                        }

                        if (_collider != null)
                        {
                                _collider.enabled = true;
                                _collider.isTrigger = false;
                                _collider.gameObject.layer = ObjectLayer.Interaction;
                        }

                        if (_networkRigidbody == null)
                        {
                                _networkRigidbody = GetComponent<NetworkRigidbody3D>();
                        }

                        if (_rigidbody == null)
                        {
                                _rigidbody = GetComponent<Rigidbody>();
                        }

                        if (_rigidbody != null)
                        {
                                _rigidbody.useGravity = true;
                                _rigidbody.isKinematic = false;
                                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        }

                        if (_networkRigidbody != null)
                        {
                                _networkRigidbody.InterpolationTarget = InterpolationTarget;
                        }

                        if (HasStateAuthority == true && _despawnTime > 0f)
                        {
                                _despawnTimer = TickTimer.CreateFromSeconds(Runner, _despawnTime);
                        }

                        EnsureDefinition();
                        RefreshVisual();
                }

                public override void FixedUpdateNetwork()
                {
                        base.FixedUpdateNetwork();

                        if (HasStateAuthority == true && _despawnTimer.IsRunning == true && _despawnTimer.Expired(Runner) == true)
                        {
                                Runner.Despawn(Object);
                                return;
                        }

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
                        _despawnTimer = default;
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

                        if (_iconDisplay == null)
                        {
                                _iconDisplay = GetComponent<ItemDefinitionIconDisplay>();
                        }

                        var graphic = _definition.WorldGraphic;
                        ClearVisual();

                        if (graphic != null)
                        {
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
                                return;
                        }

                        if (_iconDisplay != null)
                        {
                                _iconDisplay.CreateIcon(_definition, InterpolationTarget, _visualOffset);
                                _visualInitialized = _iconDisplay.HasIcon;
                        }
                }

                private void ClearVisual()
                {
                        if (_iconDisplay != null)
                        {
                                _iconDisplay.Clear();
                        }

                        if (_visualInstance != null)
                        {
                                Destroy(_visualInstance);
                                _visualInstance = null;
                        }

                        _visualInitialized = false;
                }

        // IInteraction INTERFACE

        string IInteraction.Name => Name;
        string IInteraction.Description => Description;
        Vector3 IInteraction.HUDPosition => transform.position;
        bool IInteraction.IsActive => _collider != null && _collider.enabled;

        bool IInteraction.Interact(in InteractionContext context, out string message)
        {
                Inventory inventory = context.Inventory;

                if (inventory == null && context.Interactor != null)
                {
                        inventory = context.Interactor.GetComponent<Inventory>();
                }

                if (inventory == null)
                {
                        message = "No inventory available";
                        return false;
                }

                inventory.Pickup(this);

                message = string.Empty;
                return true;
        }
        }
}
