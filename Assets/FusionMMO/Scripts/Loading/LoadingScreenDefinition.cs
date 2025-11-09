using TSS.Data;
using UnityEngine;

namespace FusionMMO.Loading
{
	[CreateAssetMenu(fileName = "LoadingScreenDefinition", menuName = "FusionMMO/Loading/Loading Screen Definition")]
	public class LoadingScreenDefinition : DataDefinition
	{
		[SerializeField]
		private string _displayName;

		[SerializeField]
		private Sprite _icon;

		[SerializeField]
		private Sprite[] _loadingScreenImages;

		public override string Name => _displayName;

		public override Sprite Icon => _icon;

		public Sprite GetRandomLoadingScreen()
		{
			if (_loadingScreenImages == null || _loadingScreenImages.Length == 0)
			{
				return null;
			}

			int index = Random.Range(0, _loadingScreenImages.Length);
			return _loadingScreenImages[index];
		}
	}
}
