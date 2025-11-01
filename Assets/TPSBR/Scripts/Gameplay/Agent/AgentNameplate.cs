using TMPro;
using UnityEngine;

namespace TPSBR
{
	public sealed class AgentNameplate : ContextBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI _nameText;

		private Agent _agent;
		private RectTransform _nameTransform;
		private bool _hasAssignedName;

		private void Awake()
		{
			if (_nameText != null)
			{
				_nameTransform = _nameText.rectTransform;
			}

			_agent = GetComponentInParent<Agent>();
		}

		public override void Spawned()
		{
			TryAssignName();
		}

		private void LateUpdate()
		{
			if (_nameTransform == null)
			{
				return;
			}

			if (_hasAssignedName == false)
			{
				_hasAssignedName = TryAssignName();
			}

			Transform cameraTransform = ResolveCameraTransform();
			if (cameraTransform == null)
			{
				return;
			}

			Vector3 forward = cameraTransform.position - _nameTransform.position;
			if (forward.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			_nameTransform.rotation = Quaternion.LookRotation(forward, cameraTransform.up);
		}

		private bool TryAssignName()
		{
			if (_nameText == null)
			{
				return true;
			}

			string displayName = GetDisplayName();
			if (displayName.HasValue() == false)
			{
				return false;
			}

			_nameText.SetTextSafe(displayName);
			return true;
		}

		private string GetDisplayName()
		{
			if (Context?.NetworkGame != null && Object != null)
			{
				Player player = Context.NetworkGame.GetPlayer(Object.InputAuthority);
				if (player != null)
				{
					if (player.CharacterName.HasValue() == true)
					{
						return player.CharacterName;
					}

					if (player.Nickname.HasValue() == true)
					{
						return player.Nickname;
					}
				}
			}

			if (_agent == null)
			{
				_agent = GetComponentInParent<Agent>();
			}

			return _agent != null ? _agent.name : null;
		}

		private Transform ResolveCameraTransform()
		{
			if (Context?.Camera != null && Context.Camera.Camera != null)
			{
				return Context.Camera.Camera.transform;
			}

			if (Camera.main != null)
			{
				return Camera.main.transform;
			}

			return null;
		}
	}
}
