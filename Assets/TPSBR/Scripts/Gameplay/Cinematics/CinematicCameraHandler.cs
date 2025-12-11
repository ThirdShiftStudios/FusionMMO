namespace TPSBR
{
    using System.Collections.Generic;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class CinematicCameraHandler : ContextBehaviour
    {
        public static CinematicCameraHandler Instance { get; private set; }

        [SerializeField]
        private List<CinematicWaypointPath> _waypointPathRoots = new List<CinematicWaypointPath>();

        [SerializeField]
        private CinematicWaypointPath __manualWaypointPath;

        public IReadOnlyList<CinematicWaypointPath> WaypointPathRoots => _waypointPathRoots;
        public bool OverrideIsActive = false;
        public bool IsActive
        {
            get => _isActive || OverrideIsActive;
            set
            {
                if (_isActive == value)
                    return;

                _isActive = value;
                UpdateSceneUIVisibility();

                if (_isActive == true)
                {
                    EnsureCurrentPath();

                    if (TryInitializeTraversal() == false)
                    {
                        _isActive = false;
                        UpdateSceneUIVisibility();
                        return;
                    }
                }
                else
                {
                    ResetTraversal();
                }
            }
        }

        public CinematicWaypointPath CurrentPath
        {
            get => _currentPath;
            set
            {
                if (_currentPath == value)
                    return;

                SetCurrentPathInternal(value);
                ResetTraversal();

                if (_isActive == true && TryInitializeTraversal() == false)
                {
                    IsActive = false;
                }
            }
        }

        private bool _isActive;
        private CinematicWaypointPath _currentPath;
        private int _segmentStartIndex;
        private float _segmentElapsed;
        private Vector3 _currentPosition;
        private Quaternion _currentRotation = Quaternion.identity;
        private bool _hasValidTransform;
        private Transform _sceneCameraTransform;
        private bool _syncHandlerTransform = true;
        private CanvasGroup _sceneUICanvasGroup;
        private bool _isSceneUIHidden;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Replacing existing {nameof(CinematicCameraHandler)} instance.");
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            RestoreSceneUIVisibility();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (IsActive == false)
                return;

            if (_currentPath == null)
            {
                SetCurrentPathInternal(__manualWaypointPath);
                ResetTraversal();
                //IsActive = false;
                //return;
            }

            var waypoints = _currentPath.Waypoints;
            if (waypoints == null || waypoints.Count == 0)
            {
                IsActive = false;
                return;
            }

            if (TryInitializeTraversal() == false)
            {
                IsActive = false;
                return;
            }

            if (_segmentStartIndex >= waypoints.Count - 1)
            {
                var finalWaypoint = waypoints[waypoints.Count - 1];
                _currentPosition = finalWaypoint.transform.position;
                _currentRotation = finalWaypoint.transform.rotation;
                if (ApplySceneCameraTransform(_currentPosition, _currentRotation) == false)
                {
                    _hasValidTransform = false;
                    IsActive = false;
                    return;
                }

                _hasValidTransform = true;
                UpdateHandlerTransform(_currentPosition, _currentRotation);

                IsActive = false;
                return;
            }

            var fromWaypoint = waypoints[_segmentStartIndex];
            var toWaypoint = waypoints[_segmentStartIndex + 1];

            float duration = Mathf.Max(toWaypoint.SegmentDuration, 0.01f);

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                deltaTime = Time.unscaledDeltaTime;
            }

            if (deltaTime <= 0f)
                return;

            _segmentElapsed += deltaTime;
            float progress = Mathf.Clamp01(_segmentElapsed / duration);

            _currentPosition = Vector3.Lerp(fromWaypoint.transform.position, toWaypoint.transform.position, progress);
            _currentRotation = Quaternion.Slerp(fromWaypoint.transform.rotation, toWaypoint.transform.rotation, progress);
            if (ApplySceneCameraTransform(_currentPosition, _currentRotation) == false)
            {
                _hasValidTransform = false;
                IsActive = false;
                return;
            }

            _hasValidTransform = true;

            UpdateHandlerTransform(_currentPosition, _currentRotation);

            if (_segmentElapsed >= duration)
            {
                _segmentStartIndex++;
                _segmentElapsed = 0f;

                if (_segmentStartIndex >= waypoints.Count - 1)
                {
                    var lastWaypoint = waypoints[waypoints.Count - 1];
                    _currentPosition = lastWaypoint.transform.position;
                    _currentRotation = lastWaypoint.transform.rotation;
                    if (ApplySceneCameraTransform(_currentPosition, _currentRotation) == false)
                    {
                        _hasValidTransform = false;
                        IsActive = false;
                        return;
                    }

                    _hasValidTransform = true;
                    UpdateHandlerTransform(_currentPosition, _currentRotation);
                }
            }
        }

        public bool TryGetCameraTransform(out Vector3 position, out Quaternion rotation)
        {
            position = _currentPosition;
            rotation = _currentRotation;
            return _hasValidTransform;
        }

        public void Play(CinematicWaypointPath path)
        {
            CurrentPath = path;
            IsActive = path != null;
        }

        public void PlayManualPath()
        {
            if (__manualWaypointPath == null)
                return;

            SetCurrentPathInternal(__manualWaypointPath);
            ResetTraversal();
            IsActive = false;
            IsActive = true;
        }

        public void StopManualPath()
        {
            if (_currentPath != __manualWaypointPath)
                return;

            IsActive = false;
        }

        public bool IsManualPathPlaying => IsActive && _currentPath == __manualWaypointPath;

        public CinematicWaypointPath ManualWaypointPath => __manualWaypointPath;

        public void ResetCurrentPathProgress()
        {
            ResetTraversal();

            if (_isActive == true)
            {
                TryInitializeTraversal();
            }
        }

        private void EnsureCurrentPath()
        {
            if (_currentPath != null)
                return;

            for (int i = 0; i < _waypointPathRoots.Count; ++i)
            {
                if (_waypointPathRoots[i] == null)
                    continue;

                SetCurrentPathInternal(_waypointPathRoots[i]);
                ResetTraversal();
                return;
            }

            if (__manualWaypointPath != null)
            {
                SetCurrentPathInternal(__manualWaypointPath);
                ResetTraversal();
            }
        }

        private void ResetTraversal()
        {
            _segmentStartIndex = 0;
            _segmentElapsed = 0f;
            _currentPosition = default;
            _currentRotation = Quaternion.identity;
            _hasValidTransform = false;
        }

        private bool TryInitializeTraversal()
        {
            if (_currentPath == null)
                return false;

            var waypoints = _currentPath.Waypoints;
            if (waypoints == null || waypoints.Count == 0)
                return false;

            if (_hasValidTransform == true)
                return true;

            if (_segmentStartIndex < 0 || _segmentStartIndex >= waypoints.Count)
            {
                _segmentStartIndex = 0;
            }

            var startingWaypoint = waypoints[_segmentStartIndex];
            _currentPosition = startingWaypoint.transform.position;
            _currentRotation = startingWaypoint.transform.rotation;
            _segmentElapsed = 0f;
            _hasValidTransform = false;

            if (ApplySceneCameraTransform(_currentPosition, _currentRotation) == false)
                return false;

            UpdateHandlerTransform(_currentPosition, _currentRotation);

            _hasValidTransform = true;

            return true;
        }

        private void SetCurrentPathInternal(CinematicWaypointPath path)
        {
            _currentPath = path;
            _syncHandlerTransform = ShouldSyncHandlerTransform(path);
        }

        private bool ShouldSyncHandlerTransform(CinematicWaypointPath path)
        {
            if (path == null)
                return true;

            Transform pathTransform = path.transform;
            if (pathTransform == null)
                return true;

            return pathTransform.IsChildOf(transform) == false;
        }

        private void UpdateHandlerTransform(Vector3 position, Quaternion rotation)
        {
            if (_syncHandlerTransform == false)
                return;

            transform.SetPositionAndRotation(position, rotation);
        }

        private Transform GetSceneCameraTransform()
        {
            if (Context?.Camera?.Camera == null)
            {
                _sceneCameraTransform = null;
                return null;
            }

            Transform cameraTransform = Context.Camera.Camera.transform;

            if (_sceneCameraTransform != cameraTransform)
            {
                _sceneCameraTransform = cameraTransform;
            }

            return _sceneCameraTransform;
        }

        private bool ApplySceneCameraTransform(Vector3 position, Quaternion rotation)
        {
            Transform cameraTransform = GetSceneCameraTransform();
            if (cameraTransform == null)
                return false;

            cameraTransform.SetPositionAndRotation(position, rotation);
            return true;
        }

        private void UpdateSceneUIVisibility()
        {
            var canvasGroup = GetSceneUICanvasGroup();
            if (canvasGroup == null)
                return;

            if (_isActive == true)
            {
                if (_isSceneUIHidden == true)
                    return;

                canvasGroup.alpha = 0f;
                _isSceneUIHidden = true;
                return;
            }

            RestoreSceneUIVisibility(canvasGroup);
        }

        private CanvasGroup GetSceneUICanvasGroup()
        {
            var canvas = Context?.UI?.Canvas;
            if (canvas == null)
            {
                _sceneUICanvasGroup = null;
                return null;
            }

            if (_sceneUICanvasGroup == null || _sceneUICanvasGroup.gameObject != canvas.gameObject)
            {
                _sceneUICanvasGroup = canvas.GetComponent<CanvasGroup>();
            }

            return _sceneUICanvasGroup;
        }

        private void RestoreSceneUIVisibility(CanvasGroup canvasGroupOverride = null)
        {
            if (_isSceneUIHidden == false)
                return;

            var canvasGroup = canvasGroupOverride ?? GetSceneUICanvasGroup();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            _isSceneUIHidden = false;
        }
    }
}

#if UNITY_EDITOR
namespace TPSBR.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(CinematicCameraHandler))]
    public class CinematicCameraHandlerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var handler = (CinematicCameraHandler)target;

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(handler.ManualWaypointPath == null))
            {
                if (handler.IsManualPathPlaying)
                {
                    if (GUILayout.Button("Stop Manual Path"))
                    {
                        handler.StopManualPath();
                    }
                }
                else
                {
                    if (GUILayout.Button("Play Manual Path"))
                    {
                        handler.PlayManualPath();
                    }
                }
            }
        }
    }
}
#endif
