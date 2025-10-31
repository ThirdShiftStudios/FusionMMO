namespace TPSBR
{
	using System;
	using UnityEngine;
	using UnityEngine.Profiling;
	using Fusion.Addons.AnimationController;
	using Fusion.Addons.KCC;

	[Serializable]
	public sealed class CharacterView
	{
		public Transform RootBone;
		public Transform HeadTransform;

                public Transform CameraHandle;
                public Transform CameraTransformHead;
                public Transform DefaultCameraTransform;
                public Transform AimCameraTransform;
                public Transform FishingCatchCameraTransform;
                public Transform InventoryOpenTransform;

		public Transform FireTransformRoot;
		public Transform FireTransform;

		public Transform LeftFoot;
		public Transform RightFoot;
	}

	[Serializable]
	public struct TransformData
	{
		public Vector3    Position;
		public Vector3    LocalPosition;
		public Quaternion Rotation;

		public TransformData(Vector3 position, Vector3 localPosition, Quaternion rotation)
		{
			Position      = position;
			LocalPosition = localPosition;
			Rotation      = rotation;
		}
	}

	public class Character : MonoBehaviour
	{
		// PUBLIC MEMBERS

		public Agent                        Agent               => _agent;
		public bool                         HasInputAuthority   => _agent.HasInputAuthority;
		public KCC                          CharacterController => _characterController;
		public CharacterAnimationController AnimationController => _animationController;
		public CharacterView                ThirdPersonView     => _thirdPersonView;

		public float DispersionMultiplier { get; private set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private CharacterView _thirdPersonView;
		[SerializeField]
		private float _cameraChangeDuration = 0.3f;

		[Header("Aim")]
		[SerializeField]
		private float _defaultFOV = 60f;
		[SerializeField]
		private float _aimFOV = 40f;
		[SerializeField]
		private float _fovChangeSpeed = 20f;

		[Header("Dispersion")]
		[SerializeField]
		private float _aimDispersionMultiplier = 0.5f;
		[SerializeField]
		private float _airDispersionMultiplier = 4f;
		[SerializeField]
		private float _runDispersionMultiplier = 5f;

		private KCC                          _characterController;
		private CharacterAnimationController _animationController;
		private Agent                        _agent;
		private SceneCamera                  _camera;

		private Vector3                      _defaultHeadOffset;
		private Vector3                      _defaultCameraOffset;

		private float                        _targetFOV;

                private float                        _cameraChangeTime;
                private float                        _cameraDistance;

                private Transform                    _overrideCameraTransform;
                private TransformData                _overrideCameraTransformData;

                private Vector3                      _defaultFireTransformPosition;

                private ECameraState                 _previousCameraState;
                private ECameraState                 _currentCameraState;
                private bool                         _inventoryOpen;
                private TransformSampler             _fireTransformSampler   = new TransformSampler();
		private TransformSampler             _cameraTransformSampler = new TransformSampler();

		// PUBLIC METHODS

		public Transform GetCameraHandle()
		{
			return _thirdPersonView.CameraHandle;
		}

		public TransformData GetCameraTransform(bool resolveRenderHistory)
		{
			TransformData transformData = GetCameraTransform(_currentCameraState);

			if (resolveRenderHistory == true && CharacterController.IsProxy == false && _cameraTransformSampler.ResolveRenderPositionAndRotation(_characterController, _agent.AgentInput.FixedInput.LocalAlpha, out Vector3 cameraPosition, out Quaternion cameraRotation) == true)
			{
				transformData.Position = cameraPosition;
				transformData.Rotation = cameraRotation;
			}

			return transformData;
		}

                public TransformData GetFireTransform(bool resolveRenderHistory)
                {
                        Vector3    firePosition;
                        Vector3    localFirePosition;
                        Quaternion fireRotation;

			if (resolveRenderHistory == true && CharacterController.IsProxy == false && _fireTransformSampler.ResolveRenderPositionAndRotation(_characterController, _agent.AgentInput.FixedInput.LocalAlpha, out firePosition, out fireRotation) == true)
			{
				localFirePosition = transform.InverseTransformPoint(firePosition);
			}
			else
			{
				_thirdPersonView.FireTransform.GetPositionAndRotation(out firePosition, out fireRotation);
				localFirePosition = transform.InverseTransformPoint(firePosition);
			}

                        return new TransformData(firePosition, localFirePosition, fireRotation);
                }

                public void SetOtherCameraAuthority(Transform cameraTransform)
                {
                        if (cameraTransform == null)
                                return;

                        if (_overrideCameraTransform != cameraTransform)
                        {
                                _overrideCameraTransform = cameraTransform;
                        }

                        SetCameraState(ECameraState.OtherAuthority);
                }

                public void ClearOtherCameraAuthority(Transform cameraTransform)
                {
                        if (cameraTransform != null && _overrideCameraTransform != cameraTransform)
                                return;

                        if (_overrideCameraTransform == null)
                                return;

                        _overrideCameraTransform = null;

                        SetCameraState(_characterController.Data.Aim == true ? ECameraState.Aim : ECameraState.Default);
                }

                public void SetInventoryOpen(bool isOpen)
                {
                        if (_inventoryOpen == isOpen)
                                return;

                        _inventoryOpen = isOpen;

                        SetCameraState(GetDesiredCameraState());
                }

                public void EnterCinematicCamera()
                {
                        SetCameraState(ECameraState.Cinematic);
                }

                public void ExitCinematicCamera()
                {
                        if (_currentCameraState == ECameraState.Cinematic)
                        {
                                SetCameraState(GetDesiredCameraState());
                        }
                }

                public void OnSpawned(Agent agent)
                {
                        _agent  = agent;
                        _camera = agent.Context.Camera;

			_characterController.SetManualUpdate(true);
			_animationController.SetManualUpdate(true);

			_previousCameraState = ECameraState.Default;
			_currentCameraState = ECameraState.Default;

                        _cameraDistance = GetCameraTransform(ECameraState.Default).LocalPosition.magnitude;

                        _fireTransformSampler.Clear();
                        _cameraTransformSampler.Clear();

                        _overrideCameraTransform     = null;
                        _overrideCameraTransformData = default;
                        _inventoryOpen               = false;
                }

		public void OnFixedUpdate()
		{
			Profiler.BeginSample(nameof(Character));

			if (_agent.Runner.IsClient == false)
			{
				int playerCount = _agent.Context.NetworkGame.ActivePlayerCount;
				if (playerCount <  50)
				{
					_animationController.SetInterlacedEvaluation(EEvaluationTarget.FixedUpdate, 1, _agent.Object.InputAuthority.AsIndex);
				}
				else if (playerCount < 100)
				{
					_animationController.SetInterlacedEvaluation(EEvaluationTarget.FixedUpdate, 2, _agent.Object.InputAuthority.AsIndex);
				}
				else if (playerCount < 150)
				{
					_animationController.SetInterlacedEvaluation(EEvaluationTarget.FixedUpdate, 4, _agent.Object.InputAuthority.AsIndex);
				}
				else
				{
					_animationController.SetInterlacedEvaluation(EEvaluationTarget.FixedUpdate, 6, _agent.Object.InputAuthority.AsIndex);
				}
			}

			if (_agent.Health.IsAlive == false)
			{
				_animationController.SetDead(true);
			}

			_characterController.ManualFixedUpdate();
			_animationController.ManualFixedUpdate();

			RefreshCameraHeadPosition();
			RefreshFiringPosition();

			TransformData fireTransformData = GetFireTransform(false);
			_fireTransformSampler.Sample(_characterController, fireTransformData.Position, fireTransformData.Rotation);

			TransformData cameraTransformData = GetCameraTransform(false);
			_cameraTransformSampler.Sample(_characterController, cameraTransformData.Position, cameraTransformData.Rotation);

			DispersionMultiplier = GetDispersionMultiplier();

			Profiler.EndSample();
		}

                public void OnRender()
                {
                        _characterController.ManualRenderUpdate();
                        _animationController.ManualRenderUpdate();

                        SetCameraState(GetDesiredCameraState());

                        RefreshCameraHeadPosition();
                        RefreshFiringPosition();

                        TransformData fireTransformData = GetFireTransform(false);
			_fireTransformSampler.Sample(_characterController, fireTransformData.Position, fireTransformData.Rotation);

			TransformData cameraTransformData = GetCameraTransform(false);
			_cameraTransformSampler.Sample(_characterController, cameraTransformData.Position, cameraTransformData.Rotation);
		}

		public void OnAgentRender()
		{
			if (_agent.IsObserved == false)
				return;

			float aimFOV = _aimFOV;
			if (_agent.Inventory.CurrentWeapon != null && _agent.Inventory.CurrentWeapon.AimFOV > 1.0f)
			{
				aimFOV = _agent.Inventory.CurrentWeapon.AimFOV;
			}

                        _targetFOV = _characterController.Data.Aim == true ? aimFOV : _defaultFOV;
                        _camera.Camera.fieldOfView = Mathf.Lerp(_camera.Camera.fieldOfView, _targetFOV, _fovChangeSpeed * Time.deltaTime);

                        if (_currentCameraState == ECameraState.OtherAuthority)
                        {
                                TransformData overrideTransform = GetCameraTransform(ECameraState.OtherAuthority);

                                _camera.transform.SetPositionAndRotation(overrideTransform.Position, overrideTransform.Rotation);
                                _cameraDistance   = 0f;
                                _cameraChangeTime = _cameraChangeDuration;

                                if (_agent.HasInputAuthority == true)
                                {
                                        _animationController.RefreshSnapping();
                                }

                                return;
                        }

                        if (_currentCameraState == ECameraState.Cinematic)
                        {
                                CinematicCameraHandler cinematicHandler = CinematicCameraHandler.Instance;

                                if (cinematicHandler != null && cinematicHandler.TryGetCameraTransform(out Vector3 cinematicPosition, out Quaternion cinematicRotation) == true)
                                {
                                        _camera.transform.SetPositionAndRotation(cinematicPosition, cinematicRotation);
                                        _cameraDistance   = 0f;
                                        _cameraChangeTime = _cameraChangeDuration;

                                        if (_agent.HasInputAuthority == true)
                                        {
                                                _animationController.RefreshSnapping();
                                        }
                                }

                                return;
                        }

                        if (_previousCameraState != _currentCameraState)
                        {
                                _cameraChangeTime += Time.deltaTime;

                                if (_cameraChangeTime >= _cameraChangeDuration)
				{
					_previousCameraState = _currentCameraState;
				}
			}

			if (_previousCameraState != _currentCameraState)
			{
				var previousCameraTransform = GetCameraTransform(_previousCameraState);
				var currentCameraTransform = GetCameraTransform(_currentCameraState);

				float progress = _cameraChangeTime / _cameraChangeDuration;

				float maxCameraDistance = currentCameraTransform.LocalPosition.magnitude;
				_cameraDistance = Mathf.Clamp(_cameraDistance + maxCameraDistance * 8.0f * Time.deltaTime, 0.0f, maxCameraDistance);

				Vector3 raycastDirection = Vector3.Normalize(currentCameraTransform.LocalPosition);
				Vector3 raycastStart     = Vector3.Lerp(previousCameraTransform.Position, currentCameraTransform.Position, progress) - raycastDirection * maxCameraDistance;
				if (_agent.Runner.GetPhysicsScene().Raycast(raycastStart, raycastDirection, out RaycastHit hitInfo, maxCameraDistance + 0.25f, -5, QueryTriggerInteraction.Ignore) == true)
				{
					Agent agent = hitInfo.transform.GetComponentInParent<Agent>();
					if (agent == null || agent != _agent)
					{
						hitInfo.distance = Mathf.Clamp(hitInfo.distance - 0.25f, 0.0f, maxCameraDistance);

						if (hitInfo.distance < _cameraDistance)
						{
							_cameraDistance = hitInfo.distance;
						}
					}
				}

				_camera.transform.position = raycastStart + raycastDirection * _cameraDistance;
				_camera.transform.rotation = Quaternion.Slerp(previousCameraTransform.Rotation, currentCameraTransform.Rotation, progress);
			}
			else
			{
				var cameraTransform = GetCameraTransform(_currentCameraState);

				float maxCameraDistance = cameraTransform.LocalPosition.magnitude;
				_cameraDistance = Mathf.Clamp(_cameraDistance + maxCameraDistance * 8.0f * Time.deltaTime, 0.0f, maxCameraDistance);

				Vector3 raycastDirection = Vector3.Normalize(cameraTransform.LocalPosition);
				Vector3 raycastStart     = cameraTransform.Position - raycastDirection * maxCameraDistance;
				if (_agent.Runner.GetPhysicsScene().Raycast(raycastStart, raycastDirection, out RaycastHit hitInfo, maxCameraDistance + 0.25f, -5, QueryTriggerInteraction.Ignore) == true)
				{
					Agent agent = hitInfo.transform.GetComponentInParent<Agent>();
					if (agent == null || agent != _agent)
					{
						hitInfo.distance = Mathf.Clamp(hitInfo.distance - 0.25f, 0.0f, maxCameraDistance);

						if (hitInfo.distance < _cameraDistance)
						{
							_cameraDistance = hitInfo.distance;
						}
					}
				}

				_camera.transform.position = raycastStart + raycastDirection * _cameraDistance;
				_camera.transform.rotation = cameraTransform.Rotation;
			}

			if (_agent.HasInputAuthority == true)
			{
				_animationController.RefreshSnapping();
			}
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_characterController = GetComponent<KCC>();
			_animationController = GetComponent<CharacterAnimationController>();

			_defaultHeadOffset   = _thirdPersonView.HeadTransform.position - transform.position;
			_defaultCameraOffset = _thirdPersonView.CameraTransformHead.position - transform.position;

			_defaultFireTransformPosition = _thirdPersonView.FireTransformRoot.localPosition;
		}

		// PRIVATE METHODS

                private TransformData GetCameraTransform(ECameraState cameraState)
                {
                        Transform cameraTransform = null;

                        switch (cameraState)
                        {
                                case ECameraState.Default:
                                        cameraTransform = _thirdPersonView.DefaultCameraTransform;
                                        break;
                                case ECameraState.Aim:
                                        cameraTransform = _thirdPersonView.AimCameraTransform;
                                        break;
                                case ECameraState.FishingCatch:
                                        cameraTransform = _thirdPersonView.FishingCatchCameraTransform;
                                        break;
                                case ECameraState.InventoryOpen:
                                        cameraTransform = _thirdPersonView.InventoryOpenTransform != null ? _thirdPersonView.InventoryOpenTransform : _thirdPersonView.DefaultCameraTransform;
                                        break;
                                case ECameraState.OtherAuthority:
                                        if (_overrideCameraTransform != null)
                                        {
                                                _overrideCameraTransformData = new TransformData(_overrideCameraTransform.position, _overrideCameraTransform.position - transform.position, _overrideCameraTransform.rotation);
                                        }

                                        return _overrideCameraTransformData;
                                case ECameraState.Cinematic:
                                        CinematicCameraHandler cinematicHandler = CinematicCameraHandler.Instance;
                                        if (cinematicHandler != null && cinematicHandler.TryGetCameraTransform(out Vector3 cinematicPosition, out Quaternion cinematicRotation) == true)
                                        {
                                                return new TransformData(cinematicPosition, transform.InverseTransformPoint(cinematicPosition), cinematicRotation);
                                        }

                                        return GetCameraTransform(ECameraState.Default);
                        }

                        if (cameraTransform == null)
                                return default;

			var transformData = new TransformData(cameraTransform.position, cameraTransform.position - cameraTransform.parent.position, cameraTransform.rotation);

			transformData.Position = transform.TransformPoint(MultiplyVector(transform.InverseTransformPoint(transformData.Position), 1.0f, 1.0f, 1.0f));
			transformData.LocalPosition = transformData.Position - transform.TransformPoint(MultiplyVector(transform.InverseTransformPoint(cameraTransform.parent.position), 1.0f, 1.0f, 1.0f));

			return transformData;
		}

		private float GetDispersionMultiplier()
		{
			float multiplier = 1f;

			bool isGrounded = _characterController.FixedData.IsGrounded == true;

			if (isGrounded == true && _characterController.FixedData.RealSpeed > 0.5f)
			{
				multiplier *= _runDispersionMultiplier;
			}

			if (isGrounded == false)
			{
				multiplier *= _airDispersionMultiplier;
			}

			if (_characterController.FixedData.Aim == true)
			{
				multiplier *= _aimDispersionMultiplier;
			}

			return multiplier;
		}

		private void RefreshCameraHeadPosition()
		{
			_thirdPersonView.RootBone.localScale = new Vector3(1.0f, 1.0f, 1.0f);

			Vector3 currentHeadOffset    = _thirdPersonView.HeadTransform.position - transform.position;
			Vector3 headOffsetDifference = currentHeadOffset - _defaultHeadOffset;
			Vector3 cameraPosition       = transform.position + _defaultHeadOffset + headOffsetDifference * 0.5f;

			Vector3 cameraHeadPosition = _thirdPersonView.CameraTransformHead.position;
			cameraHeadPosition.y = cameraPosition.y;
			_thirdPersonView.CameraTransformHead.position = cameraHeadPosition;
		}

		private void RefreshFiringPosition()
		{
			_thirdPersonView.FireTransformRoot.localPosition = _defaultFireTransformPosition;
		}

                private ECameraState GetDesiredCameraState()
                {
                        CinematicCameraHandler cinematicHandler = CinematicCameraHandler.Instance;
                        if (cinematicHandler != null && cinematicHandler.IsActive == true)
                                return ECameraState.Cinematic;

                        if (_overrideCameraTransform != null)
                                return ECameraState.OtherAuthority;

                        if (_inventoryOpen == true)
                                return ECameraState.InventoryOpen;

                        if (IsFishingCatchPullOutLoopActive() == true)
                                return ECameraState.FishingCatch;

                        if (_characterController.Data.Aim == true)
                                return ECameraState.Aim;

                        return ECameraState.Default;
                }

                private void SetCameraState(ECameraState state)
                {
                        if (state == _currentCameraState)
                                return;

			_previousCameraState = _currentCameraState;
			_currentCameraState = state;
			_cameraChangeTime = 0f;
			_cameraDistance = Mathf.Max(_cameraDistance, GetCameraTransform(_currentCameraState).LocalPosition.magnitude);
                }

                private bool IsFishingCatchPullOutLoopActive()
                {
                        UseLayer useLayer = _animationController != null ? _animationController.AttackLayer : null;
                        FishingPoleUseState fishingUseState = useLayer != null ? useLayer.FishingPoleUseState : null;

                        if (fishingUseState == null)
                                return false;

                        if (_thirdPersonView == null || _thirdPersonView.FishingCatchCameraTransform == null)
                                return false;

                        return fishingUseState.IsActive(true) == true && fishingUseState.IsCatchLoopActive == true;
                }

                private static Vector3 MultiplyVector(Vector3 vector, float x, float y, float z)
                {
                        vector.x *= x;
                        vector.y *= y;
                        vector.z *= z;
			return vector;
		}

		// HELPERS

                private enum ECameraState
                {
                        None,
                        Default,
                        Aim,
                        FishingCatch,
                        InventoryOpen,
                        OtherAuthority,
                        Cinematic,
                }
        }
}
