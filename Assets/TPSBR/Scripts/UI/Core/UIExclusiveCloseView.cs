using System.Collections.Generic;

namespace TPSBR.UI
{
    public abstract class UIExclusiveCloseView : UICloseView
    {
        private readonly List<UIView> _suppressedViews = new List<UIView>();
        private readonly List<UIView> _viewBuffer = new List<UIView>();

        protected void EnsureExclusiveOpen()
        {
            if (SceneUI == null)
                return;

            if (_suppressedViews.Count > 0)
                return;

            _viewBuffer.Clear();
            SceneUI.GetAll(_viewBuffer);

            for (int i = 0; i < _viewBuffer.Count; ++i)
            {
                UIView otherView = _viewBuffer[i];

                if (otherView == null)
                    continue;

                if (ReferenceEquals(otherView, this) == true)
                    continue;

                if (otherView.IsOpen == false)
                    continue;

                _suppressedViews.Add(otherView);
            }

            for (int i = 0; i < _suppressedViews.Count; ++i)
            {
                UIView suppressedView = _suppressedViews[i];

                suppressedView?.Close();
            }

            _viewBuffer.Clear();
        }

        protected void TryRestoreSuppressedViews()
        {
            if (_suppressedViews.Count == 0)
                return;

            if (SceneUI == null)
            {
                _suppressedViews.Clear();
                return;
            }

            if (HasOtherOpenExclusiveView() == true)
                return;

            for (int i = 0; i < _suppressedViews.Count; ++i)
            {
                UIView suppressedView = _suppressedViews[i];

                if (suppressedView == null)
                    continue;

                suppressedView.Open();
            }

            _suppressedViews.Clear();
        }

        protected override void OnDeinitialize()
        {
            _suppressedViews.Clear();
            _viewBuffer.Clear();

            base.OnDeinitialize();
        }

        private bool HasOtherOpenExclusiveView()
        {
            _viewBuffer.Clear();

            if (SceneUI == null)
                return false;

            SceneUI.GetAll(_viewBuffer);

            bool hasOther = false;

            for (int i = 0; i < _viewBuffer.Count; ++i)
            {
                UIView otherView = _viewBuffer[i];

                if (otherView == null)
                    continue;

                if (ReferenceEquals(otherView, this) == true)
                    continue;

                if (otherView.IsOpen == false)
                    continue;

                if (otherView is UIExclusiveCloseView)
                {
                    hasOther = true;
                    break;
                }
            }

            _viewBuffer.Clear();

            return hasOther;
        }
    }
}
