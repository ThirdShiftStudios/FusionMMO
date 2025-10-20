using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public sealed class MineOreState : ClipState
    {
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.1f;
        [SerializeField] private float _impactTriggerNormalizedTime = 0.85f;

        private bool _isPlaying;
        private bool _hasTriggeredThisLoop;
        private float _previousNormalizedTime;
        private OreNode _activeOreNode;
        private Agent _activeAgent;

        public bool IsPlaying => _isPlaying;

        public bool Play()
        {
            if (_isPlaying == true)
                return false;

            _isPlaying = true;

            SetAnimationTime(0.0f);
            Activate(_blendInDuration);

            ResetTriggerState();

            return true;
        }

        public void Stop()
        {
            if (_isPlaying == false)
            {
                ClearMiningTarget();
                return;
            }

            _isPlaying = false;

            ClearMiningTarget();

            Deactivate(_blendOutDuration);
        }

        public void SetMiningTarget(OreNode oreNode, Agent agent)
        {
            _activeOreNode = oreNode;
            _activeAgent = agent;

            ResetTriggerState();
        }

        public void ClearMiningTarget()
        {
            _activeOreNode = null;
            _activeAgent = null;

            ResetTriggerState();
        }

        protected override void OnInterpolate()
        {
            base.OnInterpolate();

            if (_isPlaying == false)
                return;

            if (_activeOreNode == null || _activeAgent == null)
                return;

            float normalizedTime = InterpolatedAnimationTime;

            if (normalizedTime < _previousNormalizedTime)
            {
                _hasTriggeredThisLoop = false;
            }

            bool canSendImpact = Controller != null && (Controller.HasInputAuthority == true || Controller.HasStateAuthority == true);

            if (_hasTriggeredThisLoop == false && normalizedTime >= _impactTriggerNormalizedTime)
            {
                _hasTriggeredThisLoop = true;

                if (canSendImpact == true)
                {
                    _activeOreNode.TriggerMiningImpact(_activeAgent);
                }
            }

            _previousNormalizedTime = normalizedTime;
        }

        private void ResetTriggerState()
        {
            _hasTriggeredThisLoop = false;
            _previousNormalizedTime = 0.0f;
        }
    }
}
