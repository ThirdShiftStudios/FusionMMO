using UnityEngine;

namespace TPSBR
{
    public class BuffVisual : MonoBehaviour
    {
        [SerializeField]
        private BuffDefinition _buffDefinition;
        [SerializeField]
        private GameObject _visualRoot;

        private bool _isActive;

        public BuffDefinition BuffDefinition => _buffDefinition;

        private void Awake()
        {
            if (_visualRoot == null)
            {
                _visualRoot = gameObject;
            }

            UpdateVisualState(false);
        }

        private void OnEnable()
        {
            UpdateVisualState(_isActive);
        }

        public bool MatchesDefinition(BuffDefinition definition)
        {
            return definition != null && _buffDefinition == definition;
        }

        public void SetVisualActive(bool active)
        {
            _isActive = active;
            UpdateVisualState(active);
        }

        private void UpdateVisualState(bool active)
        {
            if (_visualRoot == null)
            {
                return;
            }

            if (_visualRoot.activeSelf != active)
            {
                _visualRoot.SetActive(active);
            }
        }
    }
}
