using Fusion;
using TSS.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace TPSBR
{
    [DisallowMultipleComponent]
    public sealed class ItemDefinitionIconDisplay : MonoBehaviour
    {
        [SerializeField]
        private Vector3 _iconOffset = new Vector3(0f, 0.35f, 0f);
        [SerializeField]
        private float _iconScale = 0.5f;
        [SerializeField]
        private Material _iconMaterial;
        [SerializeField]
        private bool _useBillboard = true;
        [SerializeField]
        private Color _iconTint = Color.white;

        private GameObject _iconInstance;
        private SpriteRenderer _spriteRenderer;

        public bool HasIcon => _iconInstance != null;

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

            GameObject iconObject = new GameObject($"{definition.name}_IconDisplay");
            iconObject.transform.SetParent(parent, false);
            iconObject.transform.localPosition = _iconOffset + additionalOffset;
            iconObject.transform.localRotation = Quaternion.identity;
            iconObject.transform.localScale = Vector3.one * _iconScale;

            SpriteRenderer renderer = iconObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = _iconTint;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (_iconMaterial != null)
            {
                renderer.material = _iconMaterial;
            }

            if (_useBillboard == true && iconObject.GetComponent<FusionBasicBillboard>() == null)
            {
                iconObject.AddComponent<FusionBasicBillboard>();
            }

            _iconInstance = iconObject;
            _spriteRenderer = renderer;

            return _iconInstance;
        }

        public void UpdateDefinition(ItemDefinition definition)
        {
            if (_spriteRenderer == null)
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

            _spriteRenderer.sprite = sprite;
        }

        public void Clear()
        {
            if (_iconInstance != null)
            {
                Destroy(_iconInstance);
                _iconInstance = null;
                _spriteRenderer = null;
            }
        }
    }
}
