using UnityEngine;
using TMPro;

namespace TPSBR.UI
{
	public class UIExperienceNumberIndicatorItem : UIBehaviour
	{
		public Vector3 WorldPosition => _worldPosition;
		public bool    IsFinished    => CanvasGroup.alpha <= 0f;

		[SerializeField]
		private TextMeshProUGUI _text;
		[SerializeField]
		private Vector3 _randomOffset;
		[SerializeField]
		private string _format = "+{0} XP";

		private Vector3 _worldPosition;

		public void Activate(float value, Vector3 worldPosition)
		{
			_worldPosition = worldPosition + new Vector3(Random.Range(-_randomOffset.x, _randomOffset.x), Random.Range(-_randomOffset.y, _randomOffset.y), Random.Range(-_randomOffset.z, _randomOffset.z));

			int intValue = Mathf.RoundToInt(value);

			if (intValue == 0 && value != 0f)
			{
				intValue = value > 0f ? 1 : -1;
			}

			if (_text != null)
			{
				_text.text = string.Format(_format, intValue);
			}
		}
	}
}
