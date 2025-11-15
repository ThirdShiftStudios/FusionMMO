using Animancer;
using Fusion.Addons.FSM;
using TPSBR;
using UnityEngine;

namespace TPSBR.Enemies
{
    public abstract class SingleClipBehavior : EnemyBehavior
    {
        [SerializeField]
        private AnimationClip _enterAnimation;

        private AnimancerComponent _animancer;
        private AnimancerState _currentAnimationState;

        protected virtual AnimationClip EnterAnimation => _enterAnimation;

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

            PlayAnimation(EnterAnimation);
        }

        protected override void OnEnterStateRender()
        {
            base.OnEnterStateRender();

            if (HasStateAuthority == true)
                return;

            PlayAnimation(EnterAnimation);
        }

        protected AnimancerState PlayAnimation(ITransition transition)
        {
            if (transition == null)
                return null;

            var animancer = Animancer;
            if (animancer == null)
                return null;

            var state = animancer.Play(transition);
            if (state != null)
            {
                _currentAnimationState = state;
            }

            return state;
        }

        protected AnimancerState PlayAnimation(AnimationClip clip)
        {
            if (clip == null)
                return null;

            var animancer = Animancer;
            if (animancer == null)
                return null;

            var state = animancer.Play(clip);
            if (state != null)
            {
                _currentAnimationState = state;
            }

            return state;
        }

        protected float AnimationNormalizedTime
        {
            get
            {
                if (_currentAnimationState == null)
                    return 0f;

                var normalizedTime = _currentAnimationState.NormalizedTime;
                if (float.IsNaN(normalizedTime))
                    return 0f;

                return AnimancerUtilities.Wrap01(normalizedTime);
            }
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
