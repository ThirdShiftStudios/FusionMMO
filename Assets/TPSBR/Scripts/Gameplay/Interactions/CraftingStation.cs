using System;
using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
        public abstract class CraftingStation : ContextBehaviour, IInteraction
        {
                [Header("Interaction")]
                [SerializeField]
                private string _interactionName = "Arcane Conduit";
                [SerializeField, TextArea]
                private string _interactionDescription = "Channel mystical energies to enhance your weapons.";
                [SerializeField]
                private Transform _hudPivot;
                [SerializeField]
                private Collider _interactionCollider;
                [SerializeField]
                private Transform _cameraViewTransform;
                [SerializeField]
                private Transform _weaponViewTransform;
                [Header("Filtering")]
                public DataDefinition[] FilterDefinitions; // items must match any of these to be shown

                [Networked]
                private int SelectedItemDefinitionId { get; set; }
                [Networked]
                private ItemSourceType SelectedItemSourceType { get; set; }
                [Networked]
                private int SelectedItemSourceIndex { get; set; }

                private UIItemContextView _activeItemContextView;
                private Weapon _weaponPreviewInstance;
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
                private int _currentPreviewDefinitionId;
                private ItemSourceType _currentSelectedSourceType = ItemSourceType.None;
                private int _currentSelectedSourceIndex = -1;
                private ChangeDetector _changeDetector;

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

                        OpenItemContextView(agent);
                }

                private void OpenItemContextView(Agent agent)
                {
                        if (Context == null || Context.UI == null)
                                return;

                        UIItemContextView view = Context.UI.Get<UIItemContextView>();

                        if (view == null)
                        {
                                Debug.LogWarning($"{nameof(UIItemContextView)} is not available in the current UI setup.");
                                return;
                        }

                        view.Configure(agent, destination => PopulateItems(agent, destination));

                        if (_activeItemContextView != null)
                        {
                                _activeItemContextView.ItemSelected -= HandleItemSelected;
                                _activeItemContextView.HasClosed    -= HandleItemContextViewClosed;
                        }

                        _activeItemContextView = view;
                        _activeItemContextView.ItemSelected += HandleItemSelected;
                        _activeItemContextView.HasClosed    += HandleItemContextViewClosed;

                        Context.UI.Open(view);
                        ApplyCameraView();
                }

                private ItemStatus PopulateItems(Agent agent, List<ItemData> destination)
                {
                        if (destination == null)
                                return ItemStatus.NoItems;

                        destination.Clear();

                        if (agent == null)
                                return ItemStatus.NoAgent;

                        Inventory inventory = agent.Inventory;
                        if (inventory == null)
                                return ItemStatus.NoInventory;

                        bool hasAny = false;

                        int inventorySize = inventory.InventorySize;
                        for (int i = 0; i < inventorySize; ++i)
                        {
                                InventorySlot slot = inventory.GetItemSlot(i);
                                if (slot.IsEmpty == true)
                                        continue;

                                if (slot.GetDefinition() is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null)
                                {
                                        if (MatchesFilter(weaponDefinition) == false)
                                                continue;

                                        Sprite icon = weaponDefinition.IconSprite;
                                        destination.Add(new ItemData(icon, slot.Quantity, ItemSourceType.Inventory, i, weaponDefinition, null));
                                        hasAny = true;
                                }
                        }

                        int hotbarSize = inventory.HotbarSize;
                        for (int i = 0; i < hotbarSize; ++i)
                        {
                                Weapon weapon = inventory.GetHotbarWeapon(i);
                                if (weapon == null)
                                        continue;

                                WeaponDefinition definition = weapon.Definition as WeaponDefinition;
                                if (MatchesFilter(definition) == false)
                                        continue;

                                Sprite icon = weapon.Icon;
                                if (icon == null && definition != null)
                                {
                                        icon = definition.IconSprite;
                                }

                                destination.Add(new ItemData(icon, 1, ItemSourceType.Hotbar, i, definition, weapon));
                                hasAny = true;
                        }

                        if (hasAny == false)
                                return ItemStatus.NoItems;

                        return ItemStatus.Success;
                }

                public enum ItemStatus
                {
                        NoAgent,
                        NoInventory,
                        NoItems,
                        Success
                }

                public enum ItemSourceType
                {
                        None,
                        Inventory,
                        Hotbar
                }

                public readonly struct ItemData : IEquatable<ItemData>
                {
                        public ItemData(Sprite icon, int quantity, ItemSourceType sourceType, int sourceIndex, WeaponDefinition definition, Weapon weapon)
                        {
                                Icon = icon;
                                Quantity = quantity;
                                SourceType = sourceType;
                                SourceIndex = sourceIndex;
                                Definition = definition;
                                Weapon = weapon;
                        }

                        public Sprite Icon { get; }
                        public int Quantity { get; }
                        public ItemSourceType SourceType { get; }
                        public int SourceIndex { get; }
                        public WeaponDefinition Definition { get; }
                        public Weapon Weapon { get; }

                        public bool Equals(ItemData other)
                        {
                                return Icon == other.Icon && Quantity == other.Quantity && SourceType == other.SourceType && SourceIndex == other.SourceIndex && Definition == other.Definition && Weapon == other.Weapon;
                        }

                        public override bool Equals(object obj)
                        {
                                return obj is ItemData other && Equals(other);
                        }

                        public override int GetHashCode()
                        {
                                unchecked
                                {
                                        int hashCode = Icon != null ? Icon.GetHashCode() : 0;
                                        hashCode = (hashCode * 397) ^ Quantity;
                                        hashCode = (hashCode * 397) ^ (int)SourceType;
                                        hashCode = (hashCode * 397) ^ SourceIndex;
                                        hashCode = (hashCode * 397) ^ (Definition != null ? Definition.GetHashCode() : 0);
                                        hashCode = (hashCode * 397) ^ (Weapon != null ? Weapon.GetHashCode() : 0);
                                        return hashCode;
                                }
                        }
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

                public override void Spawned()
                {
                        base.Spawned();

                        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
                        UpdateSelectedItemSlotCache();
                        UpdateWeaponPreviewFromDefinitionId(SelectedItemDefinitionId);
                }

                private void OnDisable()
                {
                        if (_activeItemContextView != null)
                        {
                                _activeItemContextView.ItemSelected -= HandleItemSelected;
                                _activeItemContextView.HasClosed    -= HandleItemContextViewClosed;
                                _activeItemContextView = null;
                        }

                        UpdateLocalWeaponPreview(null);
                        if (HasStateAuthority == true)
                        {
                                RequestSetSelectedItem(0, ItemSourceType.None, -1);
                        }
                        else
                        {
                                _currentSelectedSourceType = ItemSourceType.None;
                                _currentSelectedSourceIndex = -1;
                        }
                        RestoreCameraView(true);
                }

                private void UpdateLocalWeaponPreview(WeaponDefinition definition)
                {
                        if (definition == null)
                        {
                                if (_currentPreviewDefinitionId != 0)
                                {
                                        ClearWeaponPreview();
                                }

                                return;
                        }

                        if (_currentPreviewDefinitionId == definition.ID)
                                return;

                        ShowWeaponPreview(definition);
                }

                private void HandleItemSelected(ItemData data)
                {
                        UpdateLocalWeaponPreview(data.Definition);
                        RequestSetSelectedItem(data.Definition != null ? data.Definition.ID : 0, data.SourceType, data.SourceIndex);
                        ApplyCameraView();
                }

                private void HandleItemContextViewClosed()
                {
                        if (_activeItemContextView != null)
                        {
                                _activeItemContextView.ItemSelected -= HandleItemSelected;
                                _activeItemContextView.HasClosed    -= HandleItemContextViewClosed;
                                _activeItemContextView = null;
                        }

                        UpdateLocalWeaponPreview(null);
                        RequestSetSelectedItem(0, ItemSourceType.None, -1);
                        RestoreCameraView();
                }

                private void ApplyCameraView()
                {
                        if (_cameraViewTransform == null || Context == null || Context.HasInput == false || Context.Camera == null || Context.Camera.Camera == null)
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

                private void ShowWeaponPreview(WeaponDefinition definition)
                {
                        if (_weaponViewTransform == null || definition == null || definition.WeaponPrefab == null)
                        {
                                ClearWeaponPreview();
                                return;
                        }

                        ClearWeaponPreview();

                        _weaponPreviewInstance = Instantiate(definition.WeaponPrefab, _weaponViewTransform);

                        DisableNetworkingForPreview(_weaponPreviewInstance);

                        Transform previewTransform = _weaponPreviewInstance.transform;
                        previewTransform.localPosition = Vector3.zero;
                        previewTransform.localRotation = Quaternion.identity;
                        previewTransform.localScale = Vector3.one;
                        _currentPreviewDefinitionId = definition.ID;
                }

                private void ClearWeaponPreview()
                {
                        if (_weaponPreviewInstance != null)
                        {
                                Destroy(_weaponPreviewInstance.gameObject);
                                _weaponPreviewInstance = null;
                        }

                        _currentPreviewDefinitionId = 0;
                }

                private void RequestSetSelectedItem(int definitionId, ItemSourceType sourceType, int sourceIndex)
                {
                        definitionId = Mathf.Max(0, definitionId);
                        sourceIndex = Mathf.Max(-1, sourceIndex);

                        if (HasStateAuthority == true)
                        {
                                SetSelectedItem(definitionId, sourceType, sourceIndex);
                                return;
                        }

                        RPC_RequestSetSelectedItem(definitionId, sourceType, sourceIndex);
                }

                [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
                private void RPC_RequestSetSelectedItem(int definitionId, ItemSourceType sourceType, int sourceIndex)
                {
                        SetSelectedItem(definitionId, sourceType, sourceIndex);
                }

                private void SetSelectedItem(int definitionId, ItemSourceType sourceType, int sourceIndex)
                {
                        definitionId = Mathf.Max(0, definitionId);
                        sourceIndex = Mathf.Max(-1, sourceIndex);

                        SelectedItemDefinitionId = definitionId;
                        SelectedItemSourceType = sourceType;
                        SelectedItemSourceIndex = sourceIndex;

                        UpdateWeaponPreviewFromDefinitionId(definitionId);
                        UpdateSelectedItemSlotCache();
                }

                private void UpdateWeaponPreviewFromDefinitionId(int definitionId)
                {
                        if (definitionId <= 0)
                        {
                                if (_currentPreviewDefinitionId != 0)
                                {
                                        ClearWeaponPreview();
                                }

                                return;
                        }

                        ItemDefinition itemDefinition = ItemDefinition.Get(definitionId);
                        if (itemDefinition is WeaponDefinition weaponDefinition)
                        {
                                if (_currentPreviewDefinitionId == weaponDefinition.ID)
                                        return;

                                ShowWeaponPreview(weaponDefinition);
                        }
                        else if (_currentPreviewDefinitionId != 0)
                        {
                                ClearWeaponPreview();
                        }
                }

                private void DisableNetworkingForPreview(Weapon previewWeapon)
                {
                        if (previewWeapon == null)
                                return;

                        NetworkBehaviour[] networkBehaviours = previewWeapon.GetComponentsInChildren<NetworkBehaviour>(true);
                        for (int i = 0; i < networkBehaviours.Length; ++i)
                        {
                                NetworkBehaviour behaviour = networkBehaviours[i];
                                behaviour.enabled = false;
                        }

                        NetworkObject[] networkObjects = previewWeapon.GetComponentsInChildren<NetworkObject>(true);
                        for (int i = 0; i < networkObjects.Length; ++i)
                        {
                                NetworkObject networkObject = networkObjects[i];
                                Destroy(networkObject);
                        }
                }

                public override void Render()
                {
                        base.Render();

                        if (_changeDetector != null)
                        {
                                foreach (var change in _changeDetector.DetectChanges(this))
                                {
                                        switch (change)
                                        {
                                                case nameof(SelectedItemDefinitionId):
                                                        UpdateWeaponPreviewFromDefinitionId(SelectedItemDefinitionId);
                                                        break;
                                                case nameof(SelectedItemSourceType):
                                                case nameof(SelectedItemSourceIndex):
                                                        UpdateSelectedItemSlotCache();
                                                        break;
                                        }
                                }
                        }

                        if (_cameraViewActive == false)
                                return;

                        UpdateCameraView();
                }

                private void UpdateSelectedItemSlotCache()
                {
                        _currentSelectedSourceType = SelectedItemSourceType;
                        _currentSelectedSourceIndex = SelectedItemSourceIndex;
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

                private bool TryGetCameraViewData(out Vector3 raycastStart, out Vector3 raycastDirection, out float maxCameraDistance, out Quaternion targetRotation)
                {
                        raycastStart = default;
                        raycastDirection = default;
                        maxCameraDistance = 0f;
                        targetRotation = Quaternion.identity;

                        if (_cameraViewTransform == null)
                                return false;

                        Transform referenceTransform = _cameraViewTransform.parent != null ? _cameraViewTransform.parent : transform;
                        Vector3 targetPosition = _cameraViewTransform.position;
                        Vector3 referencePosition = referenceTransform != null ? referenceTransform.position : Vector3.zero;
                        Vector3 offset = targetPosition - referencePosition;

                        targetRotation = _cameraViewTransform.rotation;

                        if (offset.sqrMagnitude > 0.0001f)
                        {
                                maxCameraDistance = offset.magnitude;
                                raycastDirection = offset / maxCameraDistance;
                                raycastStart = targetPosition - raycastDirection * maxCameraDistance;
                        }
                        else
                        {
                                raycastDirection = _cameraViewTransform.forward.sqrMagnitude > 0.0001f ? _cameraViewTransform.forward.normalized : Vector3.forward;
                                raycastStart = targetPosition;
                                maxCameraDistance = 0f;
                        }

                        return true;
                }

                private enum CameraTransitionState
                {
                        None,
                        Entering,
                        Active,
                        Leaving
                }

                private bool MatchesFilter(DataDefinition definition)
                {
                        if (FilterDefinitions == null || FilterDefinitions.Length == 0)
                                return true;

                        if (definition == null)
                                return false;

                        int definitionId = definition.ID;

                        for (int i = 0; i < FilterDefinitions.Length; ++i)
                        {
                                DataDefinition filter = FilterDefinitions[i];
                                if (filter == null)
                                        continue;

                                if (filter.ID == definitionId)
                                        return true;
                        }

                        return false;
                }
        }
}
