using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.FSM
{
        [DisallowMultipleComponent]
        [RequireComponent(typeof(StateMachineController))]
        public abstract class EnemyBehaviorController : NetworkBehaviour, IStateMachineOwner
        {
                public StateMachine<EnemyBehavior> Machine => _machine;

                protected IReadOnlyList<EnemyBehavior> Behaviors => _behaviors;

                [SerializeField]
                [Tooltip("Optional override for the state machine name.")]
                private string _machineName = "Enemy Behavior";

                private readonly List<EnemyBehavior> _behaviors = new(16);
                private StateMachine<EnemyBehavior> _machine;

                void IStateMachineOwner.CollectStateMachines(List<IStateMachine> stateMachines)
                {
                        CacheBehaviors();
                        InitializeBehaviors();

                        _machine = new StateMachine<EnemyBehavior>(GetMachineName(), _behaviors.ToArray());

                        ConfigureStateMachine(_machine);

                        stateMachines.Add(_machine);
                }

                protected virtual void ConfigureStateMachine(StateMachine<EnemyBehavior> machine)
                {
                        var defaultBehavior = GetDefaultBehavior(machine.States);
                        if (defaultBehavior != null)
                        {
                                machine.SetDefaultState(defaultBehavior.StateId);
                        }
                }

                protected virtual EnemyBehavior GetDefaultBehavior(EnemyBehavior[] behaviors)
                {
                        return behaviors != null && behaviors.Length > 0 ? behaviors[0] : null;
                }

                protected virtual string GetMachineName()
                {
                        if (string.IsNullOrWhiteSpace(_machineName) == false)
                                return _machineName;

                        return $"{GetType().Name} Machine";
                }

                protected virtual void CacheBehaviors()
                {
                        _behaviors.Clear();

                        var behavioursInHierarchy = GetComponentsInChildren<EnemyBehavior>(true);

                        for (int i = 0; i < behavioursInHierarchy.Length; i++)
                        {
                                if (behavioursInHierarchy[i] == null)
                                        continue;

                                _behaviors.Add(behavioursInHierarchy[i]);
                        }

                        OnBehaviorsCached(_behaviors);
                }

                protected virtual void OnBehaviorsCached(List<EnemyBehavior> behaviors) {}

                private void InitializeBehaviors()
                {
                        for (int i = 0; i < _behaviors.Count; i++)
                        {
                                _behaviors[i].SetController(this);
                        }
                }
        }
}
