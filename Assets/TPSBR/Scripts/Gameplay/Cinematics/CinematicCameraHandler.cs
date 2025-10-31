namespace TPSBR
{
        using System.Collections.Generic;
        using UnityEngine;

        [DisallowMultipleComponent]
        public class CinematicCameraHandler : MonoBehaviour
        {
                public static CinematicCameraHandler Instance { get; private set; }

                [SerializeField]
                private List<CinematicWaypointPath> _waypointPathRoots = new List<CinematicWaypointPath>();

                public IReadOnlyList<CinematicWaypointPath> WaypointPathRoots => _waypointPathRoots;

                public bool IsActive
                {
                        get => _isActive;
                        set
                        {
                                if (_isActive == value)
                                        return;

                                _isActive = value;

                                if (_isActive == true)
                                {
                                        EnsureCurrentPath();

                                        if (TryInitializeTraversal() == false)
                                        {
                                                _isActive = false;
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

                                _currentPath = value;
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
                private int _lastTraversalFrame = -1;

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
                        if (Instance == this)
                        {
                                Instance = null;
                        }
                }

                public void UpdateTraversal(float deltaTime)
                {
                        if (_isActive == false)
                                return;

                        if (deltaTime <= 0f)
                                return;

                        if (_lastTraversalFrame == Time.frameCount)
                                return;

                        _lastTraversalFrame = Time.frameCount;

                        if (_currentPath == null)
                        {
                                IsActive = false;
                                return;
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
                                _hasValidTransform = true;

                                IsActive = false;
                                return;
                        }

                        var fromWaypoint = waypoints[_segmentStartIndex];
                        var toWaypoint   = waypoints[_segmentStartIndex + 1];

                        float duration = Mathf.Max(toWaypoint.SegmentDuration, 0.01f);

                        _segmentElapsed += deltaTime;
                        float progress = Mathf.Clamp01(_segmentElapsed / duration);

                        _currentPosition = Vector3.Lerp(fromWaypoint.transform.position, toWaypoint.transform.position, progress);
                        _currentRotation = Quaternion.Slerp(fromWaypoint.transform.rotation, toWaypoint.transform.rotation, progress);
                        _hasValidTransform = true;

                        if (_segmentElapsed >= duration)
                        {
                                _segmentStartIndex++;
                                _segmentElapsed = 0f;

                                if (_segmentStartIndex >= waypoints.Count - 1)
                                {
                                        var lastWaypoint = waypoints[waypoints.Count - 1];
                                        _currentPosition = lastWaypoint.transform.position;
                                        _currentRotation = lastWaypoint.transform.rotation;
                                        _hasValidTransform = true;
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

                private void EnsureCurrentPath()
                {
                        if (_currentPath != null)
                                return;

                        for (int i = 0; i < _waypointPathRoots.Count; ++i)
                        {
                                if (_waypointPathRoots[i] == null)
                                        continue;

                                _currentPath = _waypointPathRoots[i];
                                break;
                        }
                }

                private void ResetTraversal()
                {
                        _segmentStartIndex = 0;
                        _segmentElapsed = 0f;
                        _currentPosition = default;
                        _currentRotation = Quaternion.identity;
                        _hasValidTransform = false;
                        _lastTraversalFrame = -1;
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
                        _hasValidTransform = true;

                        return true;
                }
        }
}
