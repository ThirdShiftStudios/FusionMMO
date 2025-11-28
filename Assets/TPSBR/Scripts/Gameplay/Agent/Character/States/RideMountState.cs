using Fusion.Addons.AnimationController;

namespace TPSBR
{
    public class RideMountState : ClipState
    {
        protected override void OnActivate()
        {
            base.OnActivate();
            SetAnimationCategory(AnimationCategory.FullBody);
        }
    }
}
