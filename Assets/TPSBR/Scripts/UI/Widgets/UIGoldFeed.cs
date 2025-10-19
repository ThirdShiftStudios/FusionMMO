using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
        public class UIGoldFeed : UIFeedBase
        {
                [SerializeField]
                private UIInventoryFeedItem _itemPrefab;
                [SerializeField]
                private int _poolSize = 5;
                [SerializeField]
                private Vector2 _bottomRightOffset = new Vector2(-50f, 140f);
                [SerializeField]
                private string _goldItemName = "Gold";
                [SerializeField]
                private Sprite _goldIcon;

                private readonly List<UIFeedItemBase> _items = new List<UIFeedItemBase>();

                private Inventory _inventory;
                private int _previousGold;

                public void Bind(Inventory inventory)
                {
                        if (_inventory == inventory)
                                return;

                        if (_inventory != null)
                        {
                                _inventory.GoldChanged -= OnGoldChanged;
                        }

                        _inventory = inventory;
                        _previousGold = _inventory != null ? _inventory.Gold : 0;

                        if (_inventory != null)
                        {
                                _inventory.GoldChanged += OnGoldChanged;
                        }

                        HideAll();
                }

                protected override void OnInitialize()
                {
                        EnsureItems();

                        base.OnInitialize();

                        if (RectTransform != null)
                        {
                                RectTransform.anchorMin = new Vector2(1f, 0f);
                                RectTransform.anchorMax = new Vector2(1f, 0f);
                                RectTransform.pivot     = new Vector2(1f, 0f);
                                RectTransform.anchoredPosition = _bottomRightOffset;
                        }
                }

                protected override void OnDeinitialize()
                {
                        base.OnDeinitialize();
                        Bind(null);
                }

                protected override UIFeedItemBase[] GetFeedItems()
                {
                        EnsureItems();
                        return _items.ToArray();
                }

                private void EnsureItems()
                {
                        if (_items.Count > 0)
                                return;

                        if (_itemPrefab == null)
                                return;

                        var parent = RectTransform != null ? RectTransform : (RectTransform)transform;
                        int targetCount = Mathf.Max(1, _poolSize);

                        for (int i = 0; i < targetCount; i++)
                        {
                                var item = Object.Instantiate(_itemPrefab, parent);
                                item.name = $"{_goldItemName}FeedItem_{i}";
                                item.SetActive(false);
                                _items.Add(item);
                        }
                }

                private void OnGoldChanged(int value)
                {
                        if (_inventory == null)
                                return;

                        int delta = value - _previousGold;
                        _previousGold = value;

                        if (delta == 0)
                                return;

                        var data = new InventoryFeedData
                        {
                                IsAddition     = delta > 0,
                                QuantityChange = Mathf.Abs(delta),
                                ItemName       = _goldItemName,
                                Icon           = _goldIcon,
                        };

                        ShowFeed(data);
                }
        }
}
