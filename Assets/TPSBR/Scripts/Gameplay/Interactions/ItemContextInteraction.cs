using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using UnityEngine.Serialization;

namespace TPSBR
{
        public abstract class ItemContextInteraction : ContextBehaviour, IInteraction
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
                [FormerlySerializedAs("_cameraViewTransform")]
                [SerializeField]
                private Transform _cameraTransform;

                [Header("Filtering")]
                public DataDefinition[] FilterDefinitions;

                private UIItemContextView _activeItemContextView;
                private Agent _currentAgent;
                private bool _cameraViewActive;
                private Vector3 _originalCameraPosition;
                private Quaternion _originalCameraRotation;
                private float _cameraViewDistance;

                protected UIItemContextView ActiveItemContextView => _activeItemContextView;
                protected Agent CurrentAgent => _currentAgent;
                protected Transform CameraTransform => _cameraTransform;

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

                        _currentAgent = agent;
                        _activeItemContextView = view;
                        _activeItemContextView.ItemSelected += HandleItemSelected;
                        _activeItemContextView.HasClosed    += HandleItemContextViewClosed;

                        Context.UI.Open(view);
                        OnItemContextViewOpened(agent, view);
                }

                protected virtual void OnItemContextViewOpened(Agent agent, UIItemContextView view)
                {
                        _ = agent;
                        _ = view;
                }

                protected abstract ItemStatus PopulateItems(Agent agent, List<ItemData> destination);

                protected virtual void OnItemSelected(ItemData data)
                {
                        ApplyCameraView();
                }

                protected virtual void OnItemContextViewClosed()
                {
                        RestoreCameraView();
                }

                protected virtual void OnDisable()
                {
                        if (_activeItemContextView != null)
                        {
                                _activeItemContextView.ItemSelected -= HandleItemSelected;
                                _activeItemContextView.HasClosed    -= HandleItemContextViewClosed;
                                _activeItemContextView = null;
                        }

                        OnItemContextViewClosed();
                        _currentAgent = null;
                }

                public override void Render()
                {
                        base.Render();

                        if (_cameraViewActive == false)
                                return;

                        UpdateCameraView();
                }

                private void HandleItemSelected(ItemData data)
                {
                        OnItemSelected(data);
                }

                private void HandleItemContextViewClosed()
                {
                        if (_activeItemContextView != null)
                        {
                                _activeItemContextView.ItemSelected -= HandleItemSelected;
                                _activeItemContextView.HasClosed    -= HandleItemContextViewClosed;
                                _activeItemContextView = null;
                        }

                        OnItemContextViewClosed();
                        _currentAgent = null;
                }

                protected void ApplyCameraView()
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

                protected void RestoreCameraView()
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

                protected void UpdateCameraView()
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

                protected bool MatchesFilter(DataDefinition definition)
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
#if UNITY_EDITOR
                protected virtual void OnValidate()
                {
                        if (_interactionCollider == null)
                        {
                                _interactionCollider = GetComponent<Collider>();
                        }
                }
#endif
        }
}
