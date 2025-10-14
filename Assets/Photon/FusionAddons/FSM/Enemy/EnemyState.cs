namespace Fusion.Addons.FSM
{
        public abstract class EnemyState : State<EnemyState>
        {
                public EnemyBehaviorController Controller => Behavior?.Controller;
                public EnemyBehavior Behavior { get; private set; }

                protected EnemyState(string name, int priority = 0) : base(name, priority)
                {
                }

                protected EnemyState()
                {
                }

                internal void SetBehavior(EnemyBehavior behavior)
                {
                        Behavior = behavior;
                        OnBehaviorAssigned();
                }

                protected virtual void OnBehaviorAssigned() {}
        }
}
