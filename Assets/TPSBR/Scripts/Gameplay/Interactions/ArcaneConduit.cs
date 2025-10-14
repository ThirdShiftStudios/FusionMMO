using System;
using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
        public sealed class ArcaneConduit : ContextBehaviour, IInteraction
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

                [Networked(OnChanged = nameof(OnSelectedWeaponDefinitionChanged))]
                private int SelectedWeaponDefinitionId { get; set; }

                private UIItemContextView _activeItemContextView;
                private Weapon _weaponPreviewInstance;
                private bool _cameraViewActive;
                private Vector3 _originalCameraPosition;
                private Quaternion _originalCameraRotation;
                private float _cameraViewDistance;
                private int _currentPreviewDefinitionId;

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

                        view.Configure(agent, destination => PopulateStaffItems(agent, destination));

                        if (_activeItemContextView != null)
                        {
                                _activeItemContextView.StaffItemSelected -= HandleStaffItemSelected;
                                _activeItemContextView.HasClosed         -= HandleItemContextViewClosed;
                        }

                        _activeItemContextView = view;
                        _activeItemContextView.StaffItemSelected += HandleStaffItemSelected;
                        _activeItemContextView.HasClosed         += HandleItemContextViewClosed;

                        Context.UI.Open(view);
                }

                private StaffItemStatus PopulateStaffItems(Agent agent, List<StaffItemData> destination)
                {
                        if (destination == null)
                                return StaffItemStatus.NoStaff;

                        destination.Clear();

                        if (agent == null)
                                return StaffItemStatus.NoAgent;

                        Inventory inventory = agent.Inventory;
                        if (inventory == null)
                                return StaffItemStatus.NoInventory;

                        bool hasAny = false;

                        int inventorySize = inventory.InventorySize;
                        for (int i = 0; i < inventorySize; ++i)
                        {
                                InventorySlot slot = inventory.GetItemSlot(i);
                                if (slot.IsEmpty == true)
                                        continue;

                                if (slot.GetDefinition() is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null && weaponDefinition.WeaponPrefab.Size == WeaponSize.Staff)
                                {
                                        Sprite icon = weaponDefinition.IconSprite;
                                        destination.Add(new StaffItemData(icon, slot.Quantity, StaffItemSourceType.Inventory, i, weaponDefinition, null));
                                        hasAny = true;
                                }
                        }

                        int hotbarSize = inventory.HotbarSize;
                        for (int i = 0; i < hotbarSize; ++i)
                        {
                                Weapon weapon = inventory.GetHotbarWeapon(i);
                                if (weapon == null)
                                        continue;

                                if (weapon.Size != WeaponSize.Staff)
                                        continue;

                                WeaponDefinition definition = weapon.Definition as WeaponDefinition;
                                Sprite icon = weapon.Icon;
                                if (icon == null && definition != null)
                                {
                                        icon = definition.IconSprite;
                                }

                                destination.Add(new StaffItemData(icon, 1, StaffItemSourceType.Hotbar, i, definition, weapon));
                                hasAny = true;
                        }

                        if (hasAny == false)
                                return StaffItemStatus.NoStaff;

                        return StaffItemStatus.Success;
                }

                public enum StaffItemStatus
                {
                        NoAgent,
                        NoInventory,
                        NoStaff,
                        Success
                }

                public enum StaffItemSourceType
                {
                        Inventory,
                        Hotbar
                }

                public readonly struct StaffItemData : IEquatable<StaffItemData>
                {
                        public StaffItemData(Sprite icon, int quantity, StaffItemSourceType sourceType, int sourceIndex, WeaponDefinition definition, Weapon weapon)
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
                        public StaffItemSourceType SourceType { get; }
                        public int SourceIndex { get; }
                        public WeaponDefinition Definition { get; }
                        public Weapon Weapon { get; }

                        public bool Equals(StaffItemData other)
                        {
                                return Icon == other.Icon && Quantity == other.Quantity && SourceType == other.SourceType && SourceIndex == other.SourceIndex && Definition == other.Definition && Weapon == other.Weapon;
                        }

                        public override bool Equals(object obj)
                        {
                                return obj is StaffItemData other && Equals(other);
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

                private void OnDisable()
                {
                        if (_activeItemContextView != null)
                        {
                                _activeItemContextView.StaffItemSelected -= HandleStaffItemSelected;
                                _activeItemContextView.HasClosed         -= HandleItemContextViewClosed;
                                _activeItemContextView = null;
                        }

                        UpdateLocalWeaponPreview(null);
                        if (HasStateAuthority == true)
                        {
                                RequestSetSelectedWeaponDefinition(0);
                        }
                        RestoreCameraView();
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

                private void HandleStaffItemSelected(StaffItemData data)
                {
                        UpdateLocalWeaponPreview(data.Definition);
                        RequestSetSelectedWeaponDefinition(data.Definition != null ? data.Definition.ID : 0);
                        ApplyCameraView();
                }

                private void HandleItemContextViewClosed()
                {
                        if (_activeItemContextView != null)
                        {
                                _activeItemContextView.StaffItemSelected -= HandleStaffItemSelected;
                                _activeItemContextView.HasClosed         -= HandleItemContextViewClosed;
                                _activeItemContextView = null;
                        }

                        UpdateLocalWeaponPreview(null);
                        RequestSetSelectedWeaponDefinition(0);
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

                private void RequestSetSelectedWeaponDefinition(int definitionId)
                {
                        definitionId = Mathf.Max(0, definitionId);

                        if (HasStateAuthority == true)
                        {
                                SetSelectedWeaponDefinitionId(definitionId);
                                return;
                        }

                        RPC_RequestSetSelectedWeaponDefinition(definitionId);
                }

                [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
                private void RPC_RequestSetSelectedWeaponDefinition(int definitionId)
                {
                        SetSelectedWeaponDefinitionId(definitionId);
                }

                private void SetSelectedWeaponDefinitionId(int definitionId)
                {
                        definitionId = Mathf.Max(0, definitionId);

                        if (SelectedWeaponDefinitionId == definitionId)
                        {
                                UpdateWeaponPreviewFromDefinitionId(definitionId);
                                return;
                        }

                        SelectedWeaponDefinitionId = definitionId;
                        UpdateWeaponPreviewFromDefinitionId(definitionId);
                }

                private static void OnSelectedWeaponDefinitionChanged(Changed<ArcaneConduit> changed)
                {
                        changed.Behaviour.HandleSelectedWeaponDefinitionChanged();
                }

                private void HandleSelectedWeaponDefinitionChanged()
                {
                        UpdateWeaponPreviewFromDefinitionId(SelectedWeaponDefinitionId);
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
        }
}
