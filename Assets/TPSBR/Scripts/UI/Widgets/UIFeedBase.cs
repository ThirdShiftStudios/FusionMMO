using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TPSBR.UI
{
        public interface IFeedData
        {
        }

        public abstract class UIFeedItemBase : UIBehaviour
        {
                public float VisibilityTime { get; private set; }

                public void SetData(IFeedData data)
                {
                        ApplyData(data);
                        VisibilityTime = 0f;
                }

                protected abstract void ApplyData(IFeedData data);

                protected virtual void Update()
                {
                        VisibilityTime += Time.deltaTime;
                }
        }

        public abstract class UIFeedBase : UIWidget
        {
                [SerializeField]
                private float _minVisibilityTime = 1.5f;
                [SerializeField]
                private float _maxVisibilityTime = 6f;
                [SerializeField]
                private float _moveTime = 0.3f;

                private UIFeedItemBase[] _items = System.Array.Empty<UIFeedItemBase>();
                private Vector3[]        _originalPositions = System.Array.Empty<Vector3>();
                private int              _maxFeeds;
                private Coroutine        _moveRoutine;

                private readonly List<UIFeedItemBase> _itemsPool    = new List<UIFeedItemBase>(16);
                private readonly List<UIFeedItemBase> _visibleFeeds = new List<UIFeedItemBase>(16);
                private readonly List<IFeedData>      _pendingFeeds = new List<IFeedData>(16);

                public void ShowFeed(IFeedData data)
                {
                        if (data == null)
                                return;

                        _pendingFeeds.Add(data);
                }

                public void HideAll()
                {
                        for (int i = 0; i < _items.Length; i++)
                        {
                                _items[i].SetActive(false);
                        }

                        _visibleFeeds.Clear();
                        _pendingFeeds.Clear();

                        _itemsPool.Clear();
                        _itemsPool.AddRange(_items);

                        _moveRoutine = null;
                }

                protected override void OnInitialize()
                {
                        base.OnInitialize();

                        _items = GetFeedItems();
                        _maxFeeds = _items.Length;

                        _itemsPool.Clear();
                        _itemsPool.AddRange(_items);

                        _originalPositions = new Vector3[_maxFeeds];

                        for (int i = 0; i < _maxFeeds; i++)
                        {
                                var position = _items[i].transform.position;

                                position.x /= Screen.width;
                                position.y /= Screen.height;

                                _originalPositions[i] = position;
                        }
                }

                protected override void OnVisible()
                {
                        base.OnVisible();

                        HideAll();
                }

                protected override void OnTick()
                {
                        base.OnTick();

                        if (_maxFeeds == 0)
                                return;

                        if (_moveRoutine != null)
                                return; // Do not add or remove feeds when moving

                        int visibleFeeds = _visibleFeeds.Count;

                        if (_pendingFeeds.Count > 0)
                        {
                                if (visibleFeeds == _maxFeeds)
                                {
                                        if (_visibleFeeds[0].VisibilityTime < _minVisibilityTime)
                                                return;

                                        HideFeedItem(0);
                                        return;
                                }

                                ShowFeedItem(_pendingFeeds[0]);
                                _pendingFeeds.RemoveAt(0);
                                return;
                        }

                        if (visibleFeeds > 0 && _visibleFeeds[0].VisibilityTime >= _maxVisibilityTime)
                        {
                                HideFeedItem(0);
                        }
                }

                protected virtual UIFeedItemBase[] GetFeedItems()
                {
                        return GetComponentsInChildren<UIFeedItemBase>();
                }

                private void ShowFeedItem(IFeedData data)
                {
                        int poolIndex = _itemsPool.Count - 1;

                        if (poolIndex < 0)
                                return;

                        var item = _itemsPool[poolIndex];
                        _itemsPool.RemoveAt(poolIndex);

                        _visibleFeeds.Add(item);

                        item.SetData(data);
                        item.RectTransform.position = GetPosition(_visibleFeeds.Count - 1);
                        item.SetActive(true);
                }

                private void HideFeedItem(int index)
                {
                        var feedItem = _visibleFeeds[index];

                        feedItem.SetActive(false);
                        _visibleFeeds.RemoveAt(index);

                        _itemsPool.Add(feedItem);

                        if (_visibleFeeds.Count > 0)
                        {
                                _moveRoutine = StartCoroutine(MoveFeeds_Coroutine());
                        }
                }

                private IEnumerator MoveFeeds_Coroutine()
                {
                        for (int i = 0; i < _visibleFeeds.Count; i++)
                        {
                                var feedItem = _visibleFeeds[i];

                                DOTween.Kill(feedItem);
                                feedItem.RectTransform.DOMove(GetPosition(i), _moveTime);
                        }

                        yield return new WaitForSeconds(_moveTime);

                        _moveRoutine = null;
                }

                private Vector3 GetPosition(int index)
                {
                        if (index < 0 || index >= _originalPositions.Length)
                                return Vector3.zero;

                        var position = _originalPositions[index];

                        position.x *= Screen.width;
                        position.y *= Screen.height;

                        return position;
                }
        }
}
