using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public sealed class MineOreState : ClipState
    {
        [SerializeField] private float _blendInDuration  = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.1f;

        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        
        private Agent _agent;
        private int _effectCounter;
        private int _currentAnimationCount;

        private void Awake()
        {
            //_agent = Controller.GetComponentNoAlloc<Agent>();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _agent = Controller.GetComponentNoAlloc<Agent>();
            _effectCounter = 0;
            _currentAnimationCount = 0;
        }
        
        protected override void OnInterpolate()
        {
            base.OnInterpolate();

            if (this.InterpolatedAnimationTime < 0.5f)
            {
                if (_effectCounter == _currentAnimationCount)
                {
                    _currentAnimationCount++;
                }
            }
            
            if(this.InterpolatedAnimationTime > 0.5f)
            {
                if (_effectCounter != _currentAnimationCount)
                {
                    _effectCounter++;
                    var resource = _agent.Interactions.ActiveResourceNode;
                    if (resource  is OreNode oreNode)
                    {
                        oreNode.PlayHitEffect();
                    }   
                }
            }
        }
        

        public bool Play()
        {
            if (_isPlaying == true)
                return false;

            _isPlaying = true;

            SetAnimationTime(0.0f);
            Activate(_blendInDuration);

            return true;
        }

        public void Stop()
        {
            if (_isPlaying == false)
                return;

            _isPlaying = false;

            Deactivate(_blendOutDuration);
        }
    }
}
