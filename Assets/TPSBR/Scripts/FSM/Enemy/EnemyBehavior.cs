using System;
using System.Collections.Generic;

namespace Fusion.Addons.FSM
{
    public abstract class EnemyBehavior : StateBehaviour<EnemyBehavior>
    {
        public EnemyBehaviorController Controller { get; set; }
        
    }
}