using System;
using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
        public class ItemVendor : ContextBehaviour, IInteraction
        {
                [Header("Interaction")]
                [SerializeField]
                private string _interactionName = "Item Vendor";
                [SerializeField, TextArea]
                private string _interactionDescription = "Browse randomly configured gear for sale.";
                [SerializeField]
                private Transform _hudPivot;
                [SerializeField]
                private Collider _interactionCollider;
                [SerializeField]
                private Transform _cameraTransform;

                [Header("Inventory")]
                [SerializeField]
                private DataDefinition[] _itemDefinitions;
                [SerializeField, Min(1)]
                private int _itemsPerDefinition = 3;

                private UIVendorView _activeVendorView;
                private readonly List<VendorItemData> _generatedItems = new List<VendorItemData>();

                private bool _cameraViewActive;
                private Vector3 _originalCameraPosition;
                private Quaternion _originalCameraRotation;
                private float _cameraViewDistance;
                private VendorItemData? _currentSelection;

                string  IInteraction.Name        => _interactionName;
                string  IInteraction.Description => _interactionDescription;
                Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
                bool    IInteraction.IsActive    => isActiveAndEnabled == true && (_interactionCollider == null || (_interactionCollider.enabled == true && _interactionCollider.gameObject.activeInHierarchy == true));

                public void Interact(Agent agent)
                {
                        if (agent == null)
                                return;

                        if (HasStateAuthority == false)
                                return;

                        RPC_RequestOpen(agent.Object.InputAuthority, agent.Object.Id);
                }

                [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
                private void RPC_RequestOpen(PlayerRef playerRef, NetworkId agentId)
                {
                        if (Runner == null)
                                return;

                        if (Runner.LocalPlayer != playerRef)
                                return;

                        Agent agent = null;

                        if (Runner.TryFindObject(agentId, out NetworkObject agentObject) == true)
                        {
                                agent = agentObject.GetComponent<Agent>();
                        }

                        if (agent == null && Context != null)
                        {
                                agent = Context.ObservedAgent;
                        }

                        if (agent == null)
                                return;

                        OpenVendorView(agent);
                }

                private void OpenVendorView(Agent agent)
                {
                        if (Context == null || Context.UI == null)
                                return;

                        UIVendorView view = Context.UI.Get<UIVendorView>();

                        if (view == null)
                        {
                                Debug.LogWarning($"{nameof(UIVendorView)} is not available in the current UI setup.");
                                return;
                        }

                        GenerateVendorItems();

                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                        }

                        _activeVendorView = view;
                        _activeVendorView.Configure(agent, PopulateVendorItems);
                        _activeVendorView.ItemSelected += HandleItemSelected;
                        _activeVendorView.HasClosed    += HandleVendorViewClosed;

                        Context.UI.Open(view);

                        ApplyCameraView();
                }

                private VendorItemStatus PopulateVendorItems(List<VendorItemData> destination)
                {
                        if (destination == null)
                                return VendorItemStatus.NoItems;

                        destination.Clear();

                        if (_generatedItems.Count == 0)
                        {
                                if (_itemDefinitions == null || _itemDefinitions.Length == 0)
                                        return VendorItemStatus.NoDefinitions;

                                return VendorItemStatus.NoItems;
                        }

                        destination.AddRange(_generatedItems);
                        return VendorItemStatus.Success;
                }

                private void HandleItemSelected(VendorItemData data)
                {
                        _currentSelection = data;
                }

                private void HandleVendorViewClosed()
                {
                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                                _activeVendorView = null;
                        }

                        _currentSelection = null;

                        RestoreCameraView();
                }

                private void GenerateVendorItems()
                {
                        _generatedItems.Clear();

                        if (_itemDefinitions == null)
                                return;

                        for (int i = 0; i < _itemDefinitions.Length; ++i)
                        {
                                DataDefinition definition = _itemDefinitions[i];
                                if (definition == null)
                                        continue;

                                if (definition is ItemDefinition == false)
                                        continue;

                                Sprite icon = ResolveIcon(definition);
                                int offerCount = Mathf.Max(1, _itemsPerDefinition);

                                for (int j = 0; j < offerCount; ++j)
                                {
                                        string configuration = GenerateConfiguration(definition);
                                        _generatedItems.Add(new VendorItemData(icon, 1, definition, configuration));
                                }
                        }
                }

                private static Sprite ResolveIcon(DataDefinition definition)
                {
                        if (definition is ItemDefinition itemDefinition)
                                return itemDefinition.IconSprite;

                        return null;
                }

                private static string GenerateConfiguration(DataDefinition definition)
                {
                        switch (definition)
                        {
                                case WeaponDefinition weaponDefinition when weaponDefinition.WeaponPrefab != null:
                                        return weaponDefinition.WeaponPrefab.GenerateRandomStats();
                                case PickaxeDefinition pickaxeDefinition when pickaxeDefinition.PickaxePrefab != null:
                                        return pickaxeDefinition.PickaxePrefab.GenerateRandomStats();
                                case WoodAxeDefinition woodAxeDefinition when woodAxeDefinition.WoodAxePrefab != null:
                                        return woodAxeDefinition.WoodAxePrefab.GenerateRandomStats();
                                case ItemDefinition:
                                        return string.Empty;
                                default:
                                        return string.Empty;
                        }
                }

                private void ApplyCameraView()
                {
                        if (_cameraTransform == null || Context == null || Context.HasInput == false || Context.Camera == null || Context.Camera.Camera == null)
                                return;

                        Transform cameraTransform = Context.Camera.Camera.transform;

                        if (_cameraViewActive == false)
                        {
                                _cameraViewActive = true;
                                _originalCameraPosition = cameraTransform.position;
                                _originalCameraRotation = cameraTransform.rotation;

                                if (TryGetCameraViewData(out _, out _, out float maxDistance, out _) == true)
                                {
                                        _cameraViewDistance = maxDistance;
                                }
                                else
                                {
                                        _cameraViewDistance = 0f;
                                }
                        }

                        UpdateCameraView();
                }

                private void RestoreCameraView()
                {
                        if (_cameraViewActive == false)
                                return;

                        if (Context != null && Context.Camera != null && Context.Camera.Camera != null)
                        {
                                Transform cameraTransform = Context.Camera.Camera.transform;
                                cameraTransform.SetPositionAndRotation(_originalCameraPosition, _originalCameraRotation);
                        }

                        _cameraViewActive = false;
                        _cameraViewDistance = 0f;
                }

                public override void Render()
                {
                        base.Render();

                        if (_cameraViewActive == false)
                                return;

                        UpdateCameraView();
                }

                private void UpdateCameraView()
                {
                        if (_cameraViewActive == false || Context == null || Context.HasInput == false)
                                return;

                        SceneCamera sceneCamera = Context.Camera;
                        if (sceneCamera == null || sceneCamera.Camera == null)
                                return;

                        if (TryGetCameraViewData(out Vector3 raycastStart, out Vector3 raycastDirection, out float maxCameraDistance, out Quaternion targetRotation) == false)
                                return;

                        Transform cameraTransform = sceneCamera.Camera.transform;

                        if (maxCameraDistance > 0.0001f)
                        {
                                _cameraViewDistance = Mathf.Clamp(_cameraViewDistance + maxCameraDistance * 8.0f * Time.deltaTime, 0.0f, maxCameraDistance);

                                if (Runner != null)
                                {
                                        PhysicsScene physicsScene = Runner.GetPhysicsScene();
                                        if (physicsScene.Raycast(raycastStart, raycastDirection, out RaycastHit hitInfo, maxCameraDistance + 0.25f, -5, QueryTriggerInteraction.Ignore) == true)
                                        {
                                                Agent observedAgent = Context != null ? Context.ObservedAgent : null;
                                                Agent hitAgent = hitInfo.transform.GetComponentInParent<Agent>();

                                                if (hitAgent == null || hitAgent != observedAgent)
                                                {
                                                        float adjustedDistance = Mathf.Clamp(hitInfo.distance - 0.25f, 0.0f, maxCameraDistance);
                                                        if (adjustedDistance < _cameraViewDistance)
                                                        {
                                                                _cameraViewDistance = adjustedDistance;
                                                        }
                                                }
                                        }
                                }

                                cameraTransform.position = raycastStart + raycastDirection * _cameraViewDistance;
                        }
                        else
                        {
                                _cameraViewDistance = 0f;
                                cameraTransform.position = raycastStart;
                        }

                        cameraTransform.rotation = targetRotation;
                }

                private bool TryGetCameraViewData(out Vector3 raycastStart, out Vector3 raycastDirection, out float maxCameraDistance, out Quaternion targetRotation)
                {
                        raycastStart = default;
                        raycastDirection = default;
                        maxCameraDistance = 0f;
                        targetRotation = Quaternion.identity;

                        if (_cameraTransform == null)
                                return false;

                        Transform referenceTransform = _cameraTransform.parent != null ? _cameraTransform.parent : transform;
                        Vector3 targetPosition = _cameraTransform.position;
                        Vector3 referencePosition = referenceTransform != null ? referenceTransform.position : Vector3.zero;
                        Vector3 offset = targetPosition - referencePosition;

                        targetRotation = _cameraTransform.rotation;

                        if (offset.sqrMagnitude > 0.0001f)
                        {
                                maxCameraDistance = offset.magnitude;
                                raycastDirection = offset / maxCameraDistance;
                                raycastStart = targetPosition - raycastDirection * maxCameraDistance;
                        }
                        else
                        {
                                raycastDirection = _cameraTransform.forward.sqrMagnitude > 0.0001f ? _cameraTransform.forward.normalized : Vector3.forward;
                                raycastStart = targetPosition;
                                maxCameraDistance = 0f;
                        }

                        return true;
                }

                private void OnDisable()
                {
                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                                _activeVendorView = null;
                        }

                        _currentSelection = null;
                        RestoreCameraView();
                }

#if UNITY_EDITOR
                private void OnValidate()
                {
                        if (_interactionCollider == null)
                        {
                                _interactionCollider = GetComponent<Collider>();
                        }
                }
#endif

                public enum VendorItemStatus
                {
                        NoDefinitions,
                        NoItems,
                        Success
                }

                public readonly struct VendorItemData : IEquatable<VendorItemData>
                {
                        public VendorItemData(Sprite icon, int quantity, DataDefinition definition, string configurationHash)
                        {
                                Icon = icon;
                                Quantity = quantity;
                                Definition = definition;
                                ConfigurationHash = configurationHash;
                        }

                        public Sprite Icon { get; }
                        public int Quantity { get; }
                        public DataDefinition Definition { get; }
                        public string ConfigurationHash { get; }

                        public bool Equals(VendorItemData other)
                        {
                                return Icon == other.Icon && Quantity == other.Quantity && Definition == other.Definition && string.Equals(ConfigurationHash, other.ConfigurationHash, StringComparison.Ordinal);
                        }

                        public override bool Equals(object obj)
                        {
                                return obj is VendorItemData other && Equals(other);
                        }

                        public override int GetHashCode()
                        {
                                unchecked
                                {
                                        int hashCode = Icon != null ? Icon.GetHashCode() : 0;
                                        hashCode = (hashCode * 397) ^ Quantity;
                                        hashCode = (hashCode * 397) ^ (Definition != null ? Definition.GetHashCode() : 0);
                                        hashCode = (hashCode * 397) ^ (ConfigurationHash != null ? ConfigurationHash.GetHashCode() : 0);
                                        return hashCode;
                                }
                        }
                }
        }
}
