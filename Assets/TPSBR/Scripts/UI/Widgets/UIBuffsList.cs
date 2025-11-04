using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
    public class UIBuffsList : UIWidget
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private UIBuffWidget _buffPrefab;

        private readonly List<UIBuffWidget> _items = new List<UIBuffWidget>(BuffSystem.MaxBuffSlots);

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_buffPrefab != null && _items.Contains(_buffPrefab) == false)
            {
                _items.Add(_buffPrefab);
                _buffPrefab.gameObject.SetActive(false);
            }

            if (_container == null && _buffPrefab != null)
            {
                _container = _buffPrefab.transform.parent as RectTransform;
            }
        }

        internal void Display(IReadOnlyList<BuffData> buffs)
        {
            int count = buffs != null ? buffs.Count : 0;

            EnsurePool(count);

            for (int i = 0; i < _items.Count; ++i)
            {
                UIBuffWidget widget = _items[i];
                if (widget == null)
                    continue;

                bool shouldBeActive = i < count;
                if (shouldBeActive == true)
                {
                    BuffData data = buffs[i];
                    BuffDefinition definition = BuffDefinition.Get(data.DefinitionId);

                    if (definition == null || data.IsValid == false)
                    {
                        widget.Clear();
                        widget.gameObject.SetActive(false);
                        continue;
                    }

                    widget.SetBuff(definition, data);
                    widget.gameObject.SetActive(true);
                }
                else
                {
                    widget.Clear();
                    widget.gameObject.SetActive(false);
                }
            }

            if (_container != null)
            {
                _container.gameObject.SetActive(count > 0);
            }
        }

        internal void Clear()
        {
            Display(null);
        }

        private void EnsurePool(int requiredCount)
        {
            if (_buffPrefab == null || _container == null)
                return;

            if (requiredCount <= _items.Count)
                return;

            for (int i = _items.Count; i < requiredCount; ++i)
            {
                var instance = Instantiate(_buffPrefab, _container);
                instance.gameObject.SetActive(false);
                _items.Add(instance);
            }
        }
    }
}
