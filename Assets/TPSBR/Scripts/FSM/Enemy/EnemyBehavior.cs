using System;
using System.Collections.Generic;

namespace Fusion.Addons.FSM
{
        public abstract class EnemyBehavior : StateBehaviour<EnemyBehavior>
        {
                public EnemyBehaviorController Controller { get; private set; }

                internal void SetController(EnemyBehaviorController controller)
                {
                        Controller = controller;
                        OnControllerAssigned();
                }

                protected StateMachine<EnemyState> CreateChildMachine(string name, List<IStateMachine> stateMachines, params EnemyState[] states)
                {
                        if (states != null)
                        {
                                for (int i = 0; i < states.Length; i++)
                                {
                                        states[i]?.SetBehavior(this);
                                }
                        }

                        var machine = new StateMachine<EnemyState>(name, states ?? Array.Empty<EnemyState>());

                        if (stateMachines != null)
                        {
                                stateMachines.Add(machine);
                        }

                        return machine;
                }

                protected virtual void OnControllerAssigned() {}
        }
}
