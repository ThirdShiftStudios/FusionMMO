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
                [SerializeField]
                private ItemDefinitionIconDisplay _iconDisplay;
                [SerializeField]
                private float _defaultSpawnAnimationDuration = 0.5f;
                [SerializeField]
                private float _spawnAnimationArcMultiplier = 0.35f;

                [Networked]
                public int ItemDefinitionId { get; private set; }
                [Networked]
                public byte Quantity { get; private set; }
                [Networked]
                public NetworkString<_64> ConfigurationHash { get; private set; }
                [Networked]
                private TickTimer _despawnTimer { get; set; }
                [Networked]
                private Vector3 _spawnAnimationStartPosition { get; set; }
                [Networked]
                private float _spawnAnimationDuration { get; set; }
                [Networked]
                private float _spawnAnimationArcHeight { get; set; }
                [Networked]
                private TickTimer _spawnAnimationTimer { get; set; }
                [Networked]
                private NetworkBool _spawnAnimationActive { get; set; }

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
                private bool _visualAnchorInitialized;

                public void Initialize(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash = default)
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
                        _spawnAnimationActive = false;
                        _spawnAnimationTimer = default;
                        _spawnAnimationDuration = 0f;
                        _spawnAnimationArcHeight = 0f;

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

                        EnsureInterpolationTarget();

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

                        if (HasStateAuthority == true && _spawnAnimationActive == true && _spawnAnimationTimer.IsRunning == true && _spawnAnimationTimer.Expired(Runner) == true)
                        {
                                _spawnAnimationActive = false;
                                _spawnAnimationTimer = default;
                        }

                        if (_definition == null)
                        {
                                EnsureDefinition();
                                RefreshVisual();
                        }
                }

                public override void Render()
                {
                        UpdateSpawnAnimation();

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
                        EnsureInterpolationTarget();

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
                        ResetVisualAnchor();
                }

                public void ConfigureSpawnAnimation(Vector3 worldStartPosition, float travelDuration, float arcHeight)
                {
                        if (HasStateAuthority == false)
                                return;

                        EnsureInterpolationTarget();

                        float duration = travelDuration > 0f ? travelDuration : _defaultSpawnAnimationDuration;
                        float computedArc = arcHeight >= 0f ? arcHeight : 0f;

                        _spawnAnimationStartPosition = worldStartPosition;
                        _spawnAnimationDuration = duration;
                        _spawnAnimationArcHeight = computedArc;

                        if (duration > 0f)
                        {
                                _spawnAnimationTimer = TickTimer.CreateFromSeconds(Runner, duration);
                                _spawnAnimationActive = true;
                                if (_interpolationTarget != null)
                                {
                                        _interpolationTarget.position = worldStartPosition;
                                }
                        }
                        else
                        {
                                _spawnAnimationTimer = default;
                                _spawnAnimationActive = false;
                                ResetVisualAnchor();
                        }
                }

                private void EnsureInterpolationTarget()
                {
                        if (_interpolationTarget != null)
                        {
                                if (_visualAnchorInitialized == false)
                                {
                                        ResetVisualAnchor();
                                        _visualAnchorInitialized = true;
                                }
                                return;
                        }

                        var visualRoot = new GameObject("VisualRoot");
                        visualRoot.transform.SetParent(transform, false);
                        _interpolationTarget = visualRoot.transform;
                        _visualAnchorInitialized = true;
                        ResetVisualAnchor();
                }

                private void UpdateSpawnAnimation()
                {
                        if (_interpolationTarget == null)
                                return;

                        if (_spawnAnimationActive == false)
                        {
                                ResetVisualAnchor();
                                return;
                        }

                        float duration = _spawnAnimationDuration > 0f ? _spawnAnimationDuration : _defaultSpawnAnimationDuration;
                        if (duration <= 0f)
                        {
                                ResetVisualAnchor();
                                return;
                        }

                        float remaining = _spawnAnimationTimer.IsRunning == true ? (_spawnAnimationTimer.RemainingTime(Runner) ?? 0f) : 0f;
                        float elapsed = Mathf.Clamp(duration - remaining, 0f, duration);
                        float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

                        Vector3 worldPosition = Vector3.Lerp(_spawnAnimationStartPosition, transform.position, t);

                        float arcHeight = _spawnAnimationArcHeight;
                        if (arcHeight <= 0f)
                        {
                                arcHeight = Vector3.Distance(_spawnAnimationStartPosition, transform.position) * _spawnAnimationArcMultiplier;
                        }

                        if (arcHeight > 0f)
                        {
                                worldPosition.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                        }

                        _interpolationTarget.position = worldPosition;

                        if (_spawnAnimationActive == false || (_spawnAnimationTimer.IsRunning == false && t >= 0.999f))
                        {
                                ResetVisualAnchor();
                        }
                }

                private void ResetVisualAnchor()
                {
                        if (_interpolationTarget == null || _interpolationTarget == transform)
                                return;

                        _interpolationTarget.localPosition = Vector3.zero;
                        _interpolationTarget.localRotation = Quaternion.identity;
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
