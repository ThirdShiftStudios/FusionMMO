using Fusion;
using UnityEngine;

namespace TPSBR
{
    public abstract class Tool : ContextBehaviour
    {
        private bool _isInitialized;
        private bool _isEquipped;
        private NetworkObject _owner;
        private Transform _equippedParent;
        private Transform _unequippedParent;

        public bool IsEquipped => _isEquipped;

        public void InitializeTool(NetworkObject owner, Transform equippedParent, Transform unequippedParent)
        {
            if (_isInitialized == true && _owner != owner)
                return;

            _isInitialized = true;
            _owner = owner;
            _equippedParent = equippedParent;
            _unequippedParent = unequippedParent;

            RefreshParent();
        }

        public void DeinitializeTool(NetworkObject owner)
        {
            if (_owner != null && _owner != owner)
                return;

            _isInitialized = false;
            _owner = null;
            _equippedParent = null;
            _unequippedParent = null;
        }

        public void RefreshParents(Transform equippedParent, Transform unequippedParent)
        {
            if (_isInitialized == false)
                return;

            _equippedParent = equippedParent;
            _unequippedParent = unequippedParent;

            RefreshParent();
        }

        public void SetEquipped(bool equipped)
        {
            _isEquipped = equipped;
            RefreshParent();
        }

        private void RefreshParent()
        {
            if (_isInitialized == false)
                return;

            Transform target = _isEquipped == true ? _equippedParent : _unequippedParent;
            if (target == null)
                return;

            transform.SetParent(target, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
