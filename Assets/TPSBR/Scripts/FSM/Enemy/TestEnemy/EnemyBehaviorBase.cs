using Animancer;
using Fusion.Addons.FSM;
using TPSBR;
using UnityEngine;

namespace TPSBR.Enemies
{
    public abstract class EnemyBehaviorBase : EnemyBehavior
    {
        private AnimancerComponent _animancer;

        protected virtual ITransition EnterStateAnimation => null;
        protected virtual ITransition EnterRenderAnimation => null;

        protected AnimancerComponent Animancer
        {
            get
            {
                if (_animancer == null)
                {
                    _animancer = ResolveAnimancer();
                }

                return _animancer;
            }
        }

        protected override void OnEnterState()
        {
            base.OnEnterState();

            PlayAnimation(EnterStateAnimation);
        }

        protected override void OnEnterStateRender()
        {
            base.OnEnterStateRender();

            if (HasStateAuthority == true)
                return;

            var transition = EnterRenderAnimation ?? EnterStateAnimation;
            PlayAnimation(transition);
        }

        protected AnimancerState PlayAnimation(ITransition transition)
        {
            if (transition == null)
                return null;

            var animancer = Animancer;
            if (animancer == null)
                return null;

            return animancer.Play(transition);
        }

        protected AnimancerState PlayAnimation(AnimationClip clip)
        {
            if (clip == null)
                return null;

            var animancer = Animancer;
            if (animancer == null)
                return null;

            return animancer.Play(clip);
        }

        private AnimancerComponent ResolveAnimancer()
        {
            if (TryGetComponent<AnimancerComponent>(out var animancer) == true && animancer != null)
            {
                return animancer;
            }

            if (Controller != null && Controller.TryGetComponent<AnimancerComponent>(out animancer) == true && animancer != null)
            {
                return animancer;
            }

            if (Controller != null && Controller.TryGetComponent<EnemyNetworkBehavior>(out var networkBehavior) == true && networkBehavior != null)
            {
                return networkBehavior.Animancer;
            }

            return null;
        }
    }
}
