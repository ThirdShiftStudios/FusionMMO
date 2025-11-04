using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TPSBR;

namespace TPSBR.UI
{
    public class UIBuffWidget : UIWidget
    {
        [SerializeField] private RawImage _iconImage;
        [SerializeField] private GameObject _stackRoot;
        [SerializeField] private TextMeshProUGUI _stackLabel;

        internal void SetBuff(BuffDefinition definition, BuffData data)
        {
            if (definition == null || data.IsValid == false)
            {
                Clear();
                return;
            }

            if (_iconImage != null)
            {
                _iconImage.texture = definition.Icon;
                _iconImage.enabled = definition.Icon != null;
            }

            UpdateStackLabel(definition, data);
        }

        internal void Clear()
        {
            if (_iconImage != null)
            {
                _iconImage.texture = null;
                _iconImage.enabled = false;
            }

            UpdateStackLabel(null, default);
        }

        private void UpdateStackLabel(BuffDefinition definition, BuffData data)
        {
            int stacks = data.Stacks;
            bool showStacks = definition != null && definition.IsStackable == true && stacks > 1;

            if (_stackRoot != null)
            {
                _stackRoot.SetActive(showStacks);
            }
            else if (_stackLabel != null)
            {
                _stackLabel.gameObject.SetActive(showStacks);
            }

            if (_stackLabel != null)
            {
                if (showStacks == true)
                {
                    _stackLabel.SetTextSafe(stacks.ToString());
                }
                else
                {
                    _stackLabel.SetTextSafe(string.Empty);
                }
            }
        }
    }
}
