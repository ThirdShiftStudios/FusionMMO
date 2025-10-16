using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using Pathfinding;
using UnityEngine;

namespace TPSBR.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StateMachineController))]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(TPSBR.EnemyHealth))]
    public class TestEnemy : EnemyBehaviorController
    {
        public EnemySpawningBehavior Spawning => _spawning;
        public EnemyIdleBehavior Idle => _idle;
        public EnemyPatrolBehavior Patrol => _patrol;
        public EnemyChaseAgentBehavior ChaseAgent => _chaseAgent;
        public EnemyReturnToPatrolZoneBehavior ReturnToPatrolZone => _returnToPatrolZone;
        public EnemyDeathBehavior Death => _death;
        public EnemyDespawnBehavior Despawn => _despawn;

        [SerializeField] [Tooltip("Reference to the spawning behavior.")]
        private EnemySpawningBehavior _spawning;

        [SerializeField] [Tooltip("Reference to the idle behavior.")]
        private EnemyIdleBehavior _idle;

        [SerializeField] [Tooltip("Reference to the patrol behavior.")]
        private EnemyPatrolBehavior _patrol;

        [SerializeField] [Tooltip("Reference to the chase agent behavior.")]
        private EnemyChaseAgentBehavior _chaseAgent;

        [SerializeField] [Tooltip("Reference to the return-to-patrol-zone behavior.")]
        private EnemyReturnToPatrolZoneBehavior _returnToPatrolZone;

        [SerializeField] [Tooltip("Reference to the death behavior.")]
        private EnemyDeathBehavior _death;

        [SerializeField] [Tooltip("Reference to the despawn behavior.")]
        private EnemyDespawnBehavior _despawn;

        [Header("Pathfinding")]
        [SerializeField]
        [Tooltip("Minimum horizontal distance the destination must change before requesting a new path.")]
        private float _destinationUpdateThreshold = 0.5f;

        [SerializeField]
        [Tooltip("Distance from a waypoint considered reached when following a path.")]
        private float _waypointReachedDistance = 0.35f;

        [SerializeField]
        [Tooltip("Minimum time between automatic path recalculations while following moving targets.")]
        private float _pathRecalculationInterval = 0.25f;

        private Seeker _seeker;
        private Path _currentPath;
        private Vector3 _currentDestination;
        private float _repathCooldown;
        private int _currentWaypointIndex;
        private bool _hasDestination;

        public float PatrolRadius => _patrol != null ? _patrol.PatrolRadius : 0f;

        private void Awake()
        {
            TryGetComponent(out _seeker);
        }

        protected override void OnCollectStateMachines(List<IStateMachine> stateMachines)
        {
            List<EnemyBehavior> states = new List<EnemyBehavior>();
            states.Add(_spawning);
            states.Add(_death);
            states.Add(_patrol);
            states.Add(_despawn);
            states.Add(_idle);
            states.Add(_chaseAgent);
            states.Add(_returnToPatrolZone);

            _machine = new EnemyBehaviorMachine(_machineName, this, states.ToArray());
            
            stateMachines.Add(_machine);
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            foreach (var player in Context.NetworkGame.ActivePlayers)
            {
                if (player == false) continue;
                if (player.Object == false) continue;
                if (player.IsInitialized == false) continue;
                if (player.ActiveAgent == false) continue;
                if (player.ActiveAgent.Object == false) continue;

                var agent = player.ActiveAgent;
                if (Vector3.Distance(agent.transform.position, transform.position) <= 10f)
                    _target = agent.transform;
            }
        }

        public Vector3 GetTargetPosition()
        {
            if (_target == null)
                return transform.position;

            return _target.position;
        }

        public void ResetPathfinding()
        {
            _hasDestination = false;
            _currentDestination = Vector3.zero;
            _currentWaypointIndex = 0;
            _repathCooldown = 0f;

            if (_seeker != null)
            {
                _seeker.CancelCurrentPathRequest();
            }

            ReleaseCurrentPath();
        }

        public bool MoveTowardsXZ(Vector3 target, float speed, float deltaTime)
        {
            Vector3 currentPosition = transform.position;
            target.y = currentPosition.y;

            if (_seeker == null)
            {
                return MoveDirectly(currentPosition, target, speed, deltaTime);
            }

            UpdatePathDestination(target, deltaTime);
            return FollowCurrentPath(speed, deltaTime);
        }

        public float GetHorizontalDistanceFromSpawn()
        {
            Vector3 currentPosition = transform.position;
            return GetHorizontalDistanceFromSpawn(currentPosition);
        }

        public float GetHorizontalDistanceFromSpawn(Vector3 position)
        {
            Vector3 delta = position - SpawnPosition;
            delta.y = 0f;
            return delta.magnitude;
        }

        public bool IsWithinPatrolRadius(Vector3 position)
        {
            float radius = PatrolRadius;
            if (radius <= 0f)
                return true;

            return GetHorizontalDistanceFromSpawn(position) <= radius;
        }

        private void UpdatePathDestination(Vector3 target, float deltaTime)
        {
            float thresholdSqr = _destinationUpdateThreshold * _destinationUpdateThreshold;
            bool destinationChanged = _hasDestination == false || (target - _currentDestination).sqrMagnitude > thresholdSqr;

            if (destinationChanged == true)
            {
                _currentDestination = target;
                _hasDestination = true;
                _repathCooldown = 0f;
            }
            else
            {
                _repathCooldown -= deltaTime;
            }

            if (_seeker == null)
                return;

            if ((_currentPath == null || destinationChanged == true || _repathCooldown <= 0f) && _seeker.IsDone())
            {
                _repathCooldown = _pathRecalculationInterval;
                _seeker.StartPath(transform.position, _currentDestination, OnPathComplete);
            }
        }

        private bool FollowCurrentPath(float speed, float deltaTime)
        {
            if (_hasDestination == false)
            {
                return true;
            }

            if (_currentPath == null || _currentPath.vectorPath == null || _currentPath.vectorPath.Count == 0)
            {
                return HorizontalDistance(transform.position, _currentDestination) <= _waypointReachedDistance;
            }

            Vector3 currentPosition = transform.position;

            while (_currentWaypointIndex < _currentPath.vectorPath.Count)
            {
                Vector3 waypoint = _currentPath.vectorPath[_currentWaypointIndex];
                if (HorizontalDistance(currentPosition, waypoint) > _waypointReachedDistance)
                {
                    break;
                }

                _currentWaypointIndex++;
            }

            if (_currentWaypointIndex >= _currentPath.vectorPath.Count)
            {
                transform.position = new Vector3(_currentDestination.x, currentPosition.y, _currentDestination.z);
                return HorizontalDistance(transform.position, _currentDestination) <= _waypointReachedDistance;
            }

            Vector3 nextWaypoint = _currentPath.vectorPath[_currentWaypointIndex];
            nextWaypoint.y = currentPosition.y;

            Vector3 toWaypoint = nextWaypoint - currentPosition;
            toWaypoint.y = 0f;

            float distance = toWaypoint.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                _currentWaypointIndex++;
                return FollowCurrentPath(speed, deltaTime);
            }

            float step = speed * deltaTime;
            if (step >= distance)
            {
                transform.position = new Vector3(nextWaypoint.x, currentPosition.y, nextWaypoint.z);
            }
            else
            {
                Vector3 movement = toWaypoint.normalized * step;
                transform.position = new Vector3(currentPosition.x + movement.x, currentPosition.y,
                    currentPosition.z + movement.z);
            }

            return false;
        }

        private void OnPathComplete(Path path)
        {
            if (path == null || path.error == true)
                return;

            path.Claim(this);
            ReleaseCurrentPath();

            _currentPath = path;
            _currentWaypointIndex = 0;
        }

        private bool MoveDirectly(Vector3 currentPosition, Vector3 target, float speed, float deltaTime)
        {
            Vector3 toTarget = target - currentPosition;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                transform.position = new Vector3(target.x, currentPosition.y, target.z);
                return true;
            }

            float step = speed * deltaTime;
            if (step >= distance)
            {
                transform.position = new Vector3(target.x, currentPosition.y, target.z);
                return true;
            }

            Vector3 movement = toTarget.normalized * step;
            transform.position = new Vector3(currentPosition.x + movement.x, currentPosition.y,
                currentPosition.z + movement.z);
            return false;
        }

        private float HorizontalDistance(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        private void ReleaseCurrentPath()
        {
            if (_currentPath != null)
            {
                _currentPath.Release(this);
                _currentPath = null;
            }

            _currentWaypointIndex = 0;
        }

        private void OnDestroy()
        {
            ReleaseCurrentPath();
        }
    }
}