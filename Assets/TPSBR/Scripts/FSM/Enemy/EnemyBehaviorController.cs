using System.Collections.Generic;
using TPSBR;
using UnityEngine;

namespace Fusion.Addons.FSM
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StateMachineController))]
    public abstract class EnemyBehaviorController : ContextBehaviour, IStateMachineOwner
    {
        public float MovementSpeed => _movementSpeed;

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

        public override void Spawned()
        {
            base.Spawned();

            CacheSpawnPosition();
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
    }
}