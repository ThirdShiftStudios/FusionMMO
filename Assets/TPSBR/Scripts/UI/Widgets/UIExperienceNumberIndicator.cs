using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
	public class UIExperienceNumberIndicator : UIWidget
	{
		[SerializeField]
		private UIExperienceNumberIndicatorItem _experienceItem;

		private readonly List<UIExperienceNumberIndicatorItem> _activeItems   = new List<UIExperienceNumberIndicatorItem>();
		private readonly List<UIExperienceNumberIndicatorItem> _inactiveItems = new List<UIExperienceNumberIndicatorItem>();

		private RectTransform _canvasRectTransform;
		private Canvas        _canvas;

		private readonly List<ExperienceData> _pendingExperience = new List<ExperienceData>(8);

		public void ExperienceAdded(float amount, Vector3 worldPosition)
		{
			if (amount <= 0f)
			{
				return;
			}

			_pendingExperience.Add(new ExperienceData
			{
				Amount   = amount,
				Position = worldPosition,
			});
		}

		protected override void OnInitialize()
		{
			if (_experienceItem != null)
			{
				_experienceItem.SetActive(false);
			}

			_canvas = GetComponentInParent<Canvas>();
			_canvasRectTransform = _canvas != null ? _canvas.transform as RectTransform : null;
		}

		protected override void OnHidden()
		{
			_pendingExperience.Clear();
		}

		private void Update()
		{
			for (int i = 0; i < _pendingExperience.Count; i++)
			{
				ProcessExperience(_pendingExperience[i]);
			}

			_pendingExperience.Clear();
		}

		private void LateUpdate()
		{
			UpdateActiveItems();
		}

		private void ProcessExperience(ExperienceData experienceData)
		{
			var item = _inactiveItems.PopLast();
			if (item == null)
			{
				item = Instantiate(_experienceItem);
				item.transform.SetParent(_experienceItem.transform.parent, false);
			}

			_activeItems.Add(item);

			item.Activate(experienceData.Amount, experienceData.Position);
			item.SetActive(true);
			item.transform.SetAsLastSibling();
			item.transform.position = GetUIPosition(item.WorldPosition);
		}

		private void UpdateActiveItems()
		{
			for (int i = _activeItems.Count; i --> 0;)
			{
				var item = _activeItems[i];
				if (item.IsFinished == true)
				{
					item.SetActive(false);
					_activeItems.RemoveBySwap(i);
					_inactiveItems.Add(item);
					continue;
				}

				item.transform.position = GetUIPosition(item.WorldPosition);
			}
		}

		private Vector3 GetUIPosition(Vector3 worldPosition)
		{
			if (_canvas == null || _canvasRectTransform == null)
			{
				return Vector3.zero;
			}

			var screenPoint = Context.Camera.Camera.WorldToScreenPoint(worldPosition);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, screenPoint, _canvas.worldCamera, out Vector2 screenPosition);
			return _canvasRectTransform.TransformPoint(screenPosition);
		}

		private struct ExperienceData
		{
			public float   Amount;
			public Vector3 Position;
		}
	}
}
