using System.Collections.Generic;
using Pathfinding;
using Fusion;
using Fusion.Addons.FSM;
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

        private GraphMask _activeGraphMask = GraphMask.everything;
        private Vector3 _navigationDestination;
        private bool _hasNavigationDestination;

        public float PatrolRadius => _patrol != null ? _patrol.PatrolRadius : 0f;

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

        public void StopNavigation()
        {
            if (AIPath != null)
            {
                AIPath.isStopped = true;
                AIPath.SetPath(null);
            }

            _hasNavigationDestination = false;
        }

        public void NavigateTo(Vector3 destination, float stoppingDistance)
        {
            if (AIPath == null || Seeker == null)
            {
                return;
            }

            if (Mathf.Approximately(AIPath.maxSpeed, MovementSpeed) == false)
            {
                AIPath.maxSpeed = MovementSpeed;
            }

            if (Mathf.Approximately(AIPath.endReachedDistance, stoppingDistance) == false)
            {
                AIPath.endReachedDistance = stoppingDistance;
            }

            var graphMask = DetermineClosestGraphMask(destination);
            bool graphMaskChanged = graphMask != _activeGraphMask;

            if (graphMaskChanged == true)
            {
                _activeGraphMask = graphMask;
                Seeker.graphMask = graphMask;
            }

            bool destinationChanged = _hasNavigationDestination == false ||
                                      (AIPath.destination - destination).sqrMagnitude > 0.01f;

            if (destinationChanged == true)
            {
                AIPath.destination = destination;
                _navigationDestination = destination;
                _hasNavigationDestination = true;
            }

            if ((destinationChanged == true || graphMaskChanged == true || AIPath.hasPath == false) && AIPath.pathPending == false)
            {
                AIPath.SearchPath();
            }

            AIPath.isStopped = false;
        }

        public bool HasReachedDestination(float tolerance)
        {
            if (AIPath != null)
            {
                if (AIPath.reachedDestination == true)
                    return true;

                if (tolerance > 0f)
                {
                    float sqrTolerance = tolerance * tolerance;
                    Vector3 destination = _hasNavigationDestination == true ? _navigationDestination : AIPath.destination;
                    float sqrDistance = (transform.position - destination).sqrMagnitude;
                    return sqrDistance <= sqrTolerance;
                }

                return false;
            }

            if (_hasNavigationDestination == false)
                return false;

            float sqrToleranceFallback = tolerance * tolerance;
            return (transform.position - _navigationDestination).sqrMagnitude <= sqrToleranceFallback;
        }

        public bool MoveTowardsXZ(Vector3 target, float speed, float deltaTime)
        {
            Vector3 currentPosition = transform.position;
            target.y = currentPosition.y;

            Vector3 toTarget = target - currentPosition;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance <= 0.01f)
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

        private GraphMask DetermineClosestGraphMask(Vector3 destination)
        {
            var astar = AstarPath.active;
            if (astar == null || astar.graphs == null)
                return GraphMask.everything;

            Vector3 currentPosition = transform.position;
            float bestScore = float.PositiveInfinity;
            GraphMask bestMask = GraphMask.everything;

            for (int i = 0; i < astar.graphs.Length; i++)
            {
                var graph = astar.graphs[i];
                if (graph == null || graph.active == null || graph.isScanned == false)
                    continue;

                var constraint = NearestNodeConstraint.Walkable;
                var startInfo = graph.GetNearest(currentPosition, constraint);
                if (startInfo.node == null)
                    continue;

                var endInfo = graph.GetNearest(destination, constraint);
                if (endInfo.node == null)
                    continue;

                float startDistance = (startInfo.position - currentPosition).sqrMagnitude;
                float endDistance = (endInfo.position - destination).sqrMagnitude;
                float score = startDistance + endDistance;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMask = GraphMask.FromGraph(graph);
                }
            }

            return bestMask;
        }
    }
}