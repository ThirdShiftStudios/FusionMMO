using Fusion;
using TMPro;
using UnityEngine;

namespace TPSBR
{
	public sealed class AgentNameplate : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI _nameText;

		private Agent _agent;
		private RectTransform _nameTransform;
		private bool _hasAssignedName;
		private bool _isServer;
		private NetworkObject _object;
		private void Awake()
		{
			if (_nameText != null)
			{
				_nameTransform = _nameText.rectTransform;
			}

			_agent = GetComponentInParent<Agent>();
		}

		public void OnSpawned(Agent agent, bool isServer)
		{
			_isServer = isServer;
            _agent = agent;
			_object = _agent.Object;
			if (_object.HasInputAuthority)
            {
                ClearName();
				return;
            }
            TryAssignName();
        }

        private void ClearName()
        {
			_nameText.SetTextSafe("");
        }

        public void OnDespawned()
        {
            _isServer = false;
            _agent = default;
			_hasAssignedName = false;

        }

        public void OnAgentRender()
		{
			UpdateNameplate();
		}
		private void UpdateNameplate()
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

			Vector3 forward = _nameTransform.position - cameraTransform.position;
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
			if (_agent.Context?.NetworkGame != null && _agent.Object != null)
			{
				Player player = _agent.Context.NetworkGame.GetPlayer(_agent.Object.InputAuthority);
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
			if (_agent.Context?.Camera != null && _agent.Context.Camera.Camera != null)
			{
				return _agent.Context.Camera.Camera.transform;
			}

			if (Camera.main != null)
			{
				return Camera.main.transform;
			}

			return null;
		}
	}
}
