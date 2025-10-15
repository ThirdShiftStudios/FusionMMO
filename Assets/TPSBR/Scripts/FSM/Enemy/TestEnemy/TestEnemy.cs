using System.Collections.Generic;
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
    }
}