namespace TPSBR.UI
{
	using UnityEngine;
	using UnityEngine.UI;

	public class UIMinimap : UIWidget
	{
		// CONSTANTS

		private static readonly int     ID_CIRCLE_RADIUS = Shader.PropertyToID("_CircleRadius");
		private static readonly int     ID_CIRCLE_CENTER = Shader.PropertyToID("_CircleCenter");

		private static readonly int     ID_CUTOUT_RADIUS = Shader.PropertyToID("_CutoutRadius");
		private static readonly int     ID_CUTOUT_CENTER = Shader.PropertyToID("_CutoutCenter");

		private static readonly Vector3 MAP_CENTER_SHIFT = new Vector3(0.5f, 0f, 0.5f);

		// PRIVATE MEMBERS

		[SerializeField]
		private RawImage      _mapImage;
		[SerializeField]
		private Image         _currentShrinkArea;
		[SerializeField]
		private Image         _nextShrinkArea;

		[SerializeField]
		private RectTransform _localPlayer;

		private Material      _currentAreaMaterial;
		private Material      _nextAreaMaterial;

		// UIWidget INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_currentAreaMaterial = Instantiate(_currentShrinkArea.material);
			_currentShrinkArea.material = _currentAreaMaterial;

			_nextAreaMaterial = Instantiate(_nextShrinkArea.material);
			_nextShrinkArea.material = _nextAreaMaterial;
		}

		protected override void OnDeinitialize()
		{
			if (_currentAreaMaterial != null)
			{
				Destroy(_currentAreaMaterial);
			}

			if (_nextAreaMaterial != null)
			{
				Destroy(_nextAreaMaterial);
			}
		}

		protected override void OnTick()
		{
			if (Context.Runner.Exists(Context.GameplayMode.Object) == false)
				return;

			var map        = Context.Map;

			if (map.MapTexture == null)
				return;

			_mapImage.texture = map.MapTexture;
			
			var playerTransform = Context.ObservedAgent != null ? Context.ObservedAgent.transform : Context.WaitingAgentTransform;
			if (playerTransform != null)
			{
				_localPlayer.SetActive(true);
				UpdateMinimapObject(_localPlayer, playerTransform);
			}
			else
			{
				_localPlayer.SetActive(false);
			}
		}

		// PRIVATE METHODS

		private void UpdateMinimapObject(RectTransform minimapObject, Transform objectTransform)
		{
			var map = Context.Map;
			int mapSize = Mathf.Max(map.WorldDimensions.x, map.WorldDimensions.y);

			var objectPosition = (objectTransform.position - map.transform.position) / mapSize;

			minimapObject.localPosition = new Vector2(objectPosition.x * RectTransform.sizeDelta.x, objectPosition.z * RectTransform.sizeDelta.y);
			minimapObject.rotation  = Quaternion.Euler(0f, 0f, -objectTransform.rotation.eulerAngles.y);
		}
	}
}
