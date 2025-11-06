using Fusion.Addons.AnimationController;
using UnityEngine;
namespace TPSBR
{
    public class CigaretteSmokeState : ClipState
    {
        [SerializeField] private GameObject _cigarettePack;
        [SerializeField] private GameObject _cigarette;

        [SerializeField, Range(0f, 1f)] private float _showCigarettePackTime = 0f;
        [SerializeField, Range(0f, 1f)] private float _hideCigarettePackTime = 1f;
        [SerializeField, Range(0f, 1f)] private float _showCigaretteTime = 0f;
        [SerializeField, Range(0f, 1f)] private float _hideCigaretteTime = 1f;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            SetObjectActive(_cigarettePack, false);
            SetObjectActive(_cigarette, false);
        }

        protected override void OnInterpolate()
        {
            base.OnInterpolate();

            float animationTime = Mathf.Repeat(InterpolatedAnimationTime, 1f);

            UpdateObjectVisibility(_cigarettePack, _showCigarettePackTime, _hideCigarettePackTime, animationTime);
            UpdateObjectVisibility(_cigarette, _showCigaretteTime, _hideCigaretteTime, animationTime);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            SetObjectActive(_cigarettePack, false);
            SetObjectActive(_cigarette, false);
        }

        private void UpdateObjectVisibility(GameObject target, float showTime, float hideTime, float animationTime)
        {
            if (target == null)
                return;

            showTime = Mathf.Clamp01(showTime);
            hideTime = Mathf.Clamp01(hideTime);

            bool shouldBeActive;

            if (Mathf.Approximately(showTime, hideTime))
            {
                shouldBeActive = false;
            }
            else if (showTime < hideTime)
            {
                shouldBeActive = animationTime >= showTime && animationTime < hideTime;
            }
            else
            {
                shouldBeActive = animationTime >= showTime || animationTime < hideTime;
            }

            SetObjectActive(target, shouldBeActive);
        }

        private void SetObjectActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }
    }
}
