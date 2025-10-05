using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public class UIPlayer : UIBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private TextMeshProUGUI _playerName;

		// PUBLIC MEMBERS

		public void SetData(SceneContext context, IPlayer player)
		{
			_playerName.text = player.Nickname;
		}
	}
}
