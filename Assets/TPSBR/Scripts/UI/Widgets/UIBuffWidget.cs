using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TPSBR;

namespace TPSBR.UI
{
    public class UIBuffWidget : UIWidget
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _durationImage;
        [SerializeField] private GameObject _stackRoot;
        [SerializeField] private TextMeshProUGUI _stackLabel;

        internal void SetBuff(BuffDefinition definition, BuffData data)
        {
            if (definition == null || data.IsValid == false)
            {
                Clear();
                return;
            }

            UpdateIcon(definition);
            UpdateDuration(definition, data);

            UpdateStackLabel(definition, data);
        }

        internal void Clear()
        {
            UpdateIcon(null);
            UpdateDuration(null, default);

            UpdateStackLabel(null, default);
        }

        private void UpdateIcon(BuffDefinition definition)
        {
            if (_iconImage == null)
                return;

            Sprite icon = definition != null ? definition.Icon : null;
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        private void UpdateDuration(BuffDefinition definition, BuffData data)
        {
            if (_durationImage == null)
                return;

            if (definition == null || data.IsValid == false || definition.Duration <= 0f)
            {
                _durationImage.enabled = false;
                _durationImage.fillAmount = 0f;
                return;
            }

            if (_durationImage.type != Image.Type.Filled)
            {
                _durationImage.type = Image.Type.Filled;
            }

            if (_durationImage.fillMethod != Image.FillMethod.Radial360)
            {
                _durationImage.fillMethod = Image.FillMethod.Radial360;
            }

            _durationImage.fillClockwise = false;

            int stackCount = Mathf.Max(1, definition.IsStackable ? data.Stacks : (byte)1);
            float totalDuration = Mathf.Max(Mathf.Epsilon, definition.Duration * stackCount);
            float remainingTime = Mathf.Clamp(data.RemainingTime, 0f, totalDuration);
            float normalized = remainingTime / totalDuration;

            _durationImage.fillAmount = 1f - normalized;
            _durationImage.enabled = true;
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
