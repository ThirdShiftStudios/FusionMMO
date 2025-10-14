using System;
using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using UnityEngine.Serialization;

namespace TPSBR
{
        public sealed class ItemVendor : ContextBehaviour, IInteraction
        {
                [Header("Interaction")]
                [SerializeField]
                private string _interactionName = "Item Vendor";
                [SerializeField, TextArea]
                private string _interactionDescription = "Browse and purchase configured items.";
                [SerializeField]
                private Transform _hudPivot;
                [SerializeField]
                private Collider _interactionCollider;
                [SerializeField]
                private Transform _cameraTransform;

                [Header("Filtering")]
                [FormerlySerializedAs("ItemDefinitions")]
                public DataDefinition[] FilterDefinitions;
                [SerializeField]
                private int _itemsPerDefinition = 3;

                private readonly List<VendorItemData> _generatedItems = new List<VendorItemData>();
                private bool _itemsDirty = true;

                private UIVendorView _activeVendorView;
                private bool _cameraViewActive;
                private Vector3 _originalCameraPosition;
                private Quaternion _originalCameraRotation;
                private float _cameraViewDistance;
                [SerializeField, Min(0f)]
                private float _cameraTransitionSpeed = 8f;
                [SerializeField, Min(0f)]
                private float _cameraReturnDuration = 0.25f;
                [SerializeField, Min(0f)]
                private float _cameraRotationLerpSpeed = 12f;
                private CameraTransitionState _cameraTransitionState;
                private float _cameraReturnTimer;
                private Vector3 _cameraReturnStartPosition;
                private Quaternion _cameraReturnStartRotation;

                private static WeaponDefinition[] _cachedWeaponDefinitions;
                private static PickaxeDefinition[] _cachedPickaxeDefinitions;
                private static WoodAxeDefinition[] _cachedWoodAxeDefinitions;

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

                        _itemsDirty = true;
                        view.Configure(agent, PopulateItems);

                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                        }

                        _activeVendorView = view;
                        _activeVendorView.ItemSelected += HandleItemSelected;
                        _activeVendorView.HasClosed    += HandleVendorViewClosed;

                        Context.UI.Open(view);
                        ApplyCameraView();
                }

                private VendorItemStatus PopulateItems(List<VendorItemData> destination)
                {
                        if (destination == null)
                                return VendorItemStatus.NoItems;

                        destination.Clear();

                        if (FilterDefinitions == null || FilterDefinitions.Length == 0)
                                return VendorItemStatus.NoDefinitions;

                        EnsureGeneratedItems();

                        if (_generatedItems.Count == 0)
                                return VendorItemStatus.NoItems;

                        destination.AddRange(_generatedItems);
                        return VendorItemStatus.Success;
                }

                private void EnsureGeneratedItems()
                {
                        if (_itemsDirty == false && _generatedItems.Count > 0)
                                return;

                        _generatedItems.Clear();
                        _itemsDirty = false;

                        if (FilterDefinitions == null || FilterDefinitions.Length == 0)
                                return;

                        int itemsPerDefinition = Mathf.Max(1, _itemsPerDefinition);

                        for (int i = 0; i < FilterDefinitions.Length; ++i)
                        {
                                DataDefinition filter = FilterDefinitions[i];
                                if (filter == null)
                                        continue;

                                GenerateItemsForFilter(filter, itemsPerDefinition);
                        }
                }

                private void GenerateItemsForFilter(DataDefinition filter, int count)
                {
                        if (count <= 0)
                                return;

                        if (filter is WeaponDefinition)
                        {
                                WeaponDefinition[] definitions = GetCachedDefinitions(ref _cachedWeaponDefinitions);
                                if (AddRandomItems(definitions, count, filter.GetType()) == false)
                                {
                                        AddSpecificDefinition(filter as ItemDefinition, count);
                                }
                        }
                        else if (filter is PickaxeDefinition)
                        {
                                PickaxeDefinition[] definitions = GetCachedDefinitions(ref _cachedPickaxeDefinitions);
                                if (AddRandomItems(definitions, count, filter.GetType()) == false)
                                {
                                        AddSpecificDefinition(filter as ItemDefinition, count);
                                }
                        }
                        else if (filter is WoodAxeDefinition)
                        {
                                WoodAxeDefinition[] definitions = GetCachedDefinitions(ref _cachedWoodAxeDefinitions);
                                if (AddRandomItems(definitions, count, filter.GetType()) == false)
                                {
                                        AddSpecificDefinition(filter as ItemDefinition, count);
                                }
                        }
                        else if (filter is ItemDefinition itemDefinition)
                        {
                                AddSpecificDefinition(itemDefinition, count);
                        }
                }

                private bool AddRandomItems<TDefinition>(TDefinition[] definitions, int count, Type requiredType) where TDefinition : ItemDefinition
                {
                        if (definitions == null || definitions.Length == 0)
                                return false;

                        bool addedAny = false;

                        for (int i = 0; i < count; ++i)
                        {
                                TDefinition definition = GetRandomDefinition(definitions, requiredType);
                                if (definition == null)
                                        continue;

                                AddSpecificDefinition(definition, 1);
                                addedAny = true;
                        }

                        return addedAny;
                }

                private static TDefinition GetRandomDefinition<TDefinition>(TDefinition[] definitions, Type requiredType) where TDefinition : ItemDefinition
                {
                        if (definitions == null || definitions.Length == 0)
                                return null;

                        if (requiredType == null || requiredType == typeof(TDefinition))
                        {
                                return definitions[UnityEngine.Random.Range(0, definitions.Length)];
                        }

                        int startIndex = UnityEngine.Random.Range(0, definitions.Length);

                        for (int i = 0; i < definitions.Length; ++i)
                        {
                                int index = (startIndex + i) % definitions.Length;
                                TDefinition candidate = definitions[index];

                                if (candidate != null && candidate.GetType() == requiredType)
                                {
                                        return candidate;
                                }
                        }

                        return null;
                }

                private void AddSpecificDefinition(ItemDefinition definition, int count)
                {
                        if (definition == null)
                                return;

                        Sprite icon = definition.IconSprite;

                        for (int i = 0; i < count; ++i)
                        {
                                string configurationHash = GenerateConfigurationHash(definition);
                                _generatedItems.Add(new VendorItemData(icon, 1, definition, configurationHash));
                        }
                }

                private static TDefinition[] GetCachedDefinitions<TDefinition>(ref TDefinition[] cache) where TDefinition : ItemDefinition
                {
                        if (cache == null || cache.Length == 0)
                        {
                                cache = Resources.LoadAll<TDefinition>(string.Empty);
                        }

                        return cache;
                }

                private string GenerateConfigurationHash(ItemDefinition definition)
                {
                        if (definition is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null)
                        {
                                string randomStats = weaponDefinition.WeaponPrefab.GenerateRandomStats();
                                if (string.IsNullOrWhiteSpace(randomStats) == false)
                                {
                                        return randomStats;
                                }
                        }
                        else if (definition is PickaxeDefinition pickaxeDefinition && pickaxeDefinition.PickaxePrefab != null)
                        {
                                string randomStats = pickaxeDefinition.PickaxePrefab.GenerateRandomStats();
                                if (string.IsNullOrWhiteSpace(randomStats) == false)
                                {
                                        return randomStats;
                                }
                        }
                        else if (definition is WoodAxeDefinition woodAxeDefinition && woodAxeDefinition.WoodAxePrefab != null)
                        {
                                string randomStats = woodAxeDefinition.WoodAxePrefab.GenerateRandomStats();
                                if (string.IsNullOrWhiteSpace(randomStats) == false)
                                {
                                        return randomStats;
                                }
                        }

                        return string.Empty;
                }

                private void HandleItemSelected(VendorItemData data)
                {
                        _ = data;
                        ApplyCameraView();
                }

                private void HandleVendorViewClosed()
                {
                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                                _activeVendorView = null;
                        }

                        RestoreCameraView(true);
                        _itemsDirty = true;
                        _generatedItems.Clear();
                }

                private void OnDisable()
                {
                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                                _activeVendorView = null;
                        }

                        RestoreCameraView(true);
                        _generatedItems.Clear();
                        _itemsDirty = true;
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
                                _cameraViewDistance = 0f;
                                _cameraTransitionState = CameraTransitionState.Entering;
                                _cameraReturnTimer = 0f;
                        }
                        else if (_cameraTransitionState == CameraTransitionState.Leaving)
                        {
                                _cameraTransitionState = CameraTransitionState.Entering;
                                _cameraReturnTimer = 0f;
                        }
                        else if (_cameraTransitionState == CameraTransitionState.None)
                        {
                                _cameraTransitionState = CameraTransitionState.Active;
                        }

                        UpdateCameraView();
                }

                private void RestoreCameraView(bool instant = false)
                {
                        if (_cameraViewActive == false)
                                return;

                        if (Context == null || Context.Camera == null || Context.Camera.Camera == null)
                        {
                                FinishCameraView();
                                return;
                        }

                        Transform cameraTransform = Context.Camera.Camera.transform;

                        if (instant == true || Context.HasInput == false)
                        {
                                cameraTransform.SetPositionAndRotation(_originalCameraPosition, _originalCameraRotation);
                                FinishCameraView();
                                return;
                        }

                        _cameraTransitionState = CameraTransitionState.Leaving;
                        _cameraReturnTimer = 0f;
                        _cameraReturnStartPosition = cameraTransform.position;
                        _cameraReturnStartRotation = cameraTransform.rotation;
                }

                public override void Render()
                {
                        base.Render();

                        if (_cameraViewActive == true)
                        {
                                UpdateCameraView();
                        }
                }

                private void UpdateCameraView()
                {
                        if (_cameraViewActive == false || Context == null || Context.HasInput == false)
                                return;

                        SceneCamera sceneCamera = Context.Camera;
                        if (sceneCamera == null || sceneCamera.Camera == null)
                                return;

                        Transform cameraTransform = sceneCamera.Camera.transform;

                        if (_cameraTransitionState == CameraTransitionState.Leaving)
                        {
                                UpdateCameraReturn(cameraTransform);
                                return;
                        }

                        if (TryGetCameraViewData(out Vector3 raycastStart, out Vector3 raycastDirection, out float maxCameraDistance, out Quaternion targetRotation) == false)
                                return;

                        float desiredDistance = 0f;

                        if (maxCameraDistance > 0.0001f)
                        {
                                desiredDistance = maxCameraDistance;

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
                                                        desiredDistance = Mathf.Min(desiredDistance, adjustedDistance);
                                                }
                                        }
                                }
                        }

                        float step = Mathf.Max(desiredDistance, 0.01f) * _cameraTransitionSpeed * Time.deltaTime;
                        _cameraViewDistance = Mathf.MoveTowards(_cameraViewDistance, desiredDistance, step);

                        Vector3 targetPosition = raycastStart + raycastDirection * _cameraViewDistance;
                        cameraTransform.position = targetPosition;

                        float rotationLerp = 1f - Mathf.Exp(-_cameraRotationLerpSpeed * Time.deltaTime);
                        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, rotationLerp);

                        if (_cameraTransitionState == CameraTransitionState.Entering && Mathf.Abs(_cameraViewDistance - desiredDistance) < 0.01f)
                        {
                                _cameraTransitionState = CameraTransitionState.Active;
                        }
                }

                private void UpdateCameraReturn(Transform cameraTransform)
                {
                        if (_cameraReturnDuration <= 0.0001f)
                        {
                                cameraTransform.SetPositionAndRotation(_originalCameraPosition, _originalCameraRotation);
                                FinishCameraView();
                                return;
                        }

                        _cameraReturnTimer += Time.deltaTime;

                        float t = Mathf.Clamp01(_cameraReturnTimer / _cameraReturnDuration);
                        t = t * t * (3f - 2f * t);

                        cameraTransform.position = Vector3.Lerp(_cameraReturnStartPosition, _originalCameraPosition, t);
                        cameraTransform.rotation = Quaternion.Slerp(_cameraReturnStartRotation, _originalCameraRotation, t);

                        if (t >= 0.999f)
                        {
                                cameraTransform.SetPositionAndRotation(_originalCameraPosition, _originalCameraRotation);
                                FinishCameraView();
                        }
                }

                private void FinishCameraView()
                {
                        _cameraViewActive = false;
                        _cameraTransitionState = CameraTransitionState.None;
                        _cameraViewDistance = 0f;
                        _cameraReturnTimer = 0f;
                }

                private enum CameraTransitionState
                {
                        None,
                        Entering,
                        Active,
                        Leaving
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

                public enum VendorItemStatus
                {
                        NoDefinitions,
                        NoItems,
                        Success
                }

                public readonly struct VendorItemData : IEquatable<VendorItemData>
                {
                        public VendorItemData(Sprite icon, int quantity, ItemDefinition definition, string configurationHash)
                        {
                                Icon = icon;
                                Quantity = quantity;
                                Definition = definition;
                                ConfigurationHash = configurationHash;
                        }

                        public Sprite Icon { get; }
                        public int Quantity { get; }
                        public ItemDefinition Definition { get; }
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
