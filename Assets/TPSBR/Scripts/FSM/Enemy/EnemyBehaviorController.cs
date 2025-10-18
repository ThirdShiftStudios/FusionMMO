using System;
using System.Collections.Generic;
using Pathfinding;
using TPSBR;
using UnityEngine;

namespace Fusion.Addons.FSM
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StateMachineController))]
    public abstract class EnemyBehaviorController : ContextBehaviour, IStateMachineOwner
    {
        public float MovementSpeed => _movementSpeed;
        public AIPath AIPath => _aiPath;
        public Seeker Seeker => _seeker;
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        public bool HasPlayerTarget => _target != null;

        public Vector3 SpawnPosition
        {
            get
            {
                CacheSpawnPosition();
                return _spawnPosition;
            }
        }
        
        [SerializeField] [Tooltip("Optional explicit target to chase when entering the chase state.")]
        private protected Transform _target;
        public EnemyBehaviorMachine Machine => _machine;
        [Header("Movement")] [SerializeField] [Tooltip("Movement speed used by simple navigation logic.")]
        private float _movementSpeed = 3f;
        

        private Vector3 _spawnPosition;
        private bool _hasSpawnPosition;
        private AIPath _aiPath;
        private Seeker _seeker;

        protected IReadOnlyList<EnemyBehavior> Behaviors => _behaviors;

        [SerializeField] [Tooltip("Optional override for the state machine name.")]
        private protected string _machineName = "Enemy Behavior";

        private readonly List<EnemyBehavior> _behaviors = new(16);
        private protected EnemyBehaviorMachine _machine;

        void IStateMachineOwner.CollectStateMachines(List<IStateMachine> stateMachines)
        {
            OnCollectStateMachines(stateMachines);
        }

        protected abstract void OnCollectStateMachines(List<IStateMachine> stateMachines);

        private void Awake()
        {
            //CacheNavigationComponents();
        }

        public override void Spawned()
        {
            base.Spawned();

            CacheSpawnPosition();
            CacheNavigationComponents();
        }
        public void ClearTarget()
        {
            _target = null;
        }
        private void CacheSpawnPosition()
        {
            if (_hasSpawnPosition == true)
                return;

            _spawnPosition = transform.position;
            _hasSpawnPosition = true;
        }

        private void CacheNavigationComponents()
        {
            _aiPath = GetComponent<AIPath>();
            _seeker = GetComponent<Seeker>();

            if (_aiPath != null)
            {
                _aiPath.maxSpeed = _movementSpeed;
                _aiPath.canSearch = true;

#pragma warning disable CS0618 // Backwards compatibility - keep simulateMovement in sync
                _aiPath.canMove = true;
#pragma warning restore CS0618

                _aiPath.simulateMovement = false;
                _aiPath.updatePosition = true;
                _aiPath.isStopped = true;
                _aiPath.SetPath(null);
                _aiPath.Teleport(transform.position);
            }
        }
    }
}