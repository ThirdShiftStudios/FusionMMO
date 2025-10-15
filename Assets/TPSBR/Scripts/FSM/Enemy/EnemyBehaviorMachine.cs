namespace Fusion.Addons.FSM
{
    public class EnemyBehaviorMachine : StateMachine<EnemyBehavior>
    {
        public EnemyBehaviorMachine(string name, EnemyBehaviorController controller, params EnemyBehavior[] states) : base(name, states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                states[i].Controller = controller;
            }
        }
    }
}