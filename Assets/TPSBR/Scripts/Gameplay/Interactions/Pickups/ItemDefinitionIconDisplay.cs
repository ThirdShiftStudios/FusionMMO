using Fusion;
using TSS.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TPSBR
{
    [DisallowMultipleComponent]
    public sealed class ItemDefinitionIconDisplay : MonoBehaviour
    {
        [SerializeField]
        private Vector3 _iconOffset = new Vector3(0f, 0.35f, 0f);
        
        [SerializeField]
        private bool _useBillboard = true;
        [SerializeField]
        private Color _iconTint = Color.white;
        [SerializeField]
        Image _icon;

        bool _hasIcon = false;
        public bool HasIcon => _hasIcon;

        public GameObject CreateIcon(ItemDefinition definition, Transform parent, Vector3 additionalOffset)
        {
            Clear();

            if (definition == null)
            {
                return null;
            }

            Sprite sprite = definition.IconSprite;
            if (sprite == null)
            {
                return null;
            }

            _icon.sprite = sprite;

          
            if (_useBillboard == true && _icon.GetComponent<FusionBasicBillboard>() == null)
            {
                _icon.gameObject.AddComponent<FusionBasicBillboard>();
            }
            _hasIcon = true;
            return _icon.gameObject;
        }

        public void UpdateDefinition(ItemDefinition definition)
        {
            if (_icon == null)
            {
                return;
            }

            if (definition == null)
            {
                Clear();
                return;
            }

            Sprite sprite = definition.IconSprite;
            if (sprite == null)
            {
                Clear();
                return;
            }
        }

        public void Clear()
        {
            _hasIcon = false;
            if (_icon != null)
            {
                _icon.sprite = null;
            }
        }
    }
}
