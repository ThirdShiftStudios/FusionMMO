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

        [Header("Movement")]
        [SerializeField]
        [Tooltip("Movement speed used by simple navigation logic.")]
        private float _movementSpeed = 3f;

        [SerializeField]
        [Tooltip("Optional explicit target to chase when entering the chase state.")]
        private Transform _playerTarget;

        private Vector3 _spawnPosition;
        private bool _hasSpawnPosition;

        protected override void OnBehaviorsCached(List<EnemyBehavior> behaviors)
        {
            base.OnBehaviorsCached(behaviors);

            _spawning = ResolveBehavior(_spawning, behaviors);
            _idle = ResolveBehavior(_idle, behaviors);
            _patrol = ResolveBehavior(_patrol, behaviors);
            _chaseAgent = ResolveBehavior(_chaseAgent, behaviors);
            _returnToPatrolZone = ResolveBehavior(_returnToPatrolZone, behaviors);
            _death = ResolveBehavior(_death, behaviors);
            _despawn = ResolveBehavior(_despawn, behaviors);
        }

        public override void Spawned()
        {
            base.Spawned();

            CacheSpawnPosition();
        }

        public float MovementSpeed => _movementSpeed;

        public Transform PlayerTarget
        {
            get => _playerTarget;
            set => _playerTarget = value;
        }

        public bool HasPlayerTarget => _playerTarget != null;

        public Vector3 SpawnPosition
        {
            get
            {
                CacheSpawnPosition();
                return _spawnPosition;
            }
        }

        public float PatrolRadius => _patrol != null ? _patrol.PatrolRadius : 0f;

        public Vector3 GetPlayerPosition()
        {
            if (_playerTarget == null)
                return transform.position;

            return _playerTarget.position;
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
            transform.position = new Vector3(currentPosition.x + movement.x, currentPosition.y, currentPosition.z + movement.z);
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

        private void CacheSpawnPosition()
        {
            if (_hasSpawnPosition == true)
                return;

            _spawnPosition = transform.position;
            _hasSpawnPosition = true;
        }

        protected override void ConfigureStateMachine(StateMachine<EnemyBehavior> machine)
        {
            base.ConfigureStateMachine(machine);

            if (_spawning != null)
            {
                machine.SetDefaultState(_spawning.StateId);
            }
        }

        private static TBehavior ResolveBehavior<TBehavior>(TBehavior current, IReadOnlyList<EnemyBehavior> available)
            where TBehavior : EnemyBehavior
        {
            if (current != null)
                return current;

            for (int i = 0; i < available.Count; i++)
            {
                if (available[i] is TBehavior typed)
                {
                    return typed;
                }
            }

            return null;
        }
    }
}