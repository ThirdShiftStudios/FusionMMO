using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class SceneSwitcher : MonoBehaviour, IContextBehaviour
    {
        [SerializeField, ScenePath]
        private string _scenePath;

        [SerializeField, Min(0f)]
        private float _triggerDistance = 3f;

        private SceneContext _context;
        private bool _switchRequested;
        private float _triggerDistanceSqr;

        public SceneContext Context
        {
            get => _context;
            set => _context = value;
        }

        private void Awake()
        {
            _triggerDistanceSqr = _triggerDistance * _triggerDistance;
        }

        private void OnValidate()
        {
            _triggerDistance = Mathf.Max(0f, _triggerDistance);
            _triggerDistanceSqr = _triggerDistance * _triggerDistance;
#if UNITY_EDITOR
            _scenePath = NormalizeScenePath(_scenePath);
#endif
        }

        private void Update()
        {
            if (_switchRequested == true)
                return;

            if (_context == null)
                return;

            if (_scenePath.HasValue() == false)
                return;

            var runner = _context.Runner;
            if (runner == null || runner.IsShutdown)
                return;

            var agent = _context.ObservedAgent;
            if (agent == null)
                return;

            if (agent.Object == null || agent.Object.HasInputAuthority == false)
                return;

            float distanceSqr = (agent.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr > _triggerDistanceSqr)
                return;

            RequestSceneSwitch(runner);
        }

        private void RequestSceneSwitch(NetworkRunner runner)
        {
            var networking = Global.Networking;
            if (networking == null || networking.IsConnected == false)
                return;

            string normalizedScenePath = NormalizeScenePath(_scenePath);
            if (normalizedScenePath.HasValue() == false)
            {
                Debug.LogWarning($"{nameof(SceneSwitcher)} on {name} cannot request a scene switch because the target scene path is not set.", this);
                return;
            }

            var request = BuildSessionRequest(runner, normalizedScenePath);

            _switchRequested = true;
            networking.StartGame(request);
        }

        private SessionRequest BuildSessionRequest(NetworkRunner runner, string scenePath)
        {
            var playerData = _context?.PlayerData;
            var sessionInfo = runner.SessionInfo;
            bool hasSessionInfo = sessionInfo.IsValid;

            string userID = playerData != null && playerData.UserID.HasValue()
                ? playerData.UserID
                : Guid.NewGuid().ToString();

            string sessionName = hasSessionInfo && sessionInfo.Name.HasValue()
                ? sessionInfo.Name
                : Guid.NewGuid().ToString();

            string displayName = hasSessionInfo ? sessionInfo.GetDisplayName() : null;
            if (displayName.HasValue() == false && playerData != null)
            {
                displayName = playerData.Nickname.HasValue() ? playerData.Nickname : playerData.CharacterName;
            }

            if (displayName.HasValue() == false)
            {
                displayName = userID;
            }

            GameMode gameMode = hasSessionInfo ? sessionInfo.GetGameMode() : runner.GameMode;
            //if (gameMode == GameMode.None)
            {
                gameMode = runner.GameMode;
            }

            EGameplayType gameplayType = hasSessionInfo ? sessionInfo.GetGameplayType() : EGameplayType.None;
            if (gameplayType == EGameplayType.None && _context?.GameplayMode != null)
            {
                gameplayType = _context.GameplayMode.Type;
            }

            var request = new SessionRequest
            {
                UserID = userID,
                DisplayName = displayName,
                GameMode = gameMode,
                SessionName = sessionName,
                ScenePath = scenePath,
                GameplayType = gameplayType,
                MaxPlayers = hasSessionInfo ? sessionInfo.MaxPlayers : 0,
            };

            return request;
        }

        private static string NormalizeScenePath(string scenePath)
        {
            if (scenePath.HasValue() == false)
                return scenePath;

            const string prefix = "Assets/";
            const string suffix = ".unity";

            if (scenePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && scenePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return scenePath.Substring(prefix.Length, scenePath.Length - prefix.Length - suffix.Length);
            }

            return scenePath;
        }
    }
}
