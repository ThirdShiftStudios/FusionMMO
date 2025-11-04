using System;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPSBR
{
    public class SceneSwitcher : MonoBehaviour, IContextBehaviour
    {
        [SerializeField, ScenePath]
        private string _scenePath;

        [SerializeField, Min(0f)]
        private float _triggerDistance = 3f;

        [SerializeField]
        private SceneSwitchMode _switchMode = SceneSwitchMode.Toggle;

        [SerializeField]
        private LocalPhysicsMode _localPhysicsMode = LocalPhysicsMode.Physics3D;

        [SerializeField]
        private bool _setActiveOnLoad = true;

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
            string normalizedScenePath = NormalizeScenePath(_scenePath);
            if (normalizedScenePath.HasValue() == false)
            {
                Debug.LogWarning($"{nameof(SceneSwitcher)} on {name} cannot request a scene switch because the target scene path is not set.", this);
                return;
            }

            if (RunnerAdditiveSceneManager.TryResolveSceneRef(runner, normalizedScenePath, out var sceneRef) == false)
            {
                Debug.LogWarning($"{nameof(SceneSwitcher)} on {name} failed to resolve scene '{normalizedScenePath}'. Ensure the scene is added to build settings.", this);
                return;
            }

            if (RunnerAdditiveSceneManager.IsSceneOperationInProgress(runner, sceneRef) == true)
                return;

            SceneSwitchMode desiredMode = _switchMode;
            if (desiredMode == SceneSwitchMode.Toggle && RunnerAdditiveSceneManager.IsSceneLoaded(runner, sceneRef) == true)
            {
                desiredMode = SceneSwitchMode.Unload;
            }

            if (desiredMode == SceneSwitchMode.Unload && RunnerAdditiveSceneManager.IsSceneLoaded(runner, sceneRef) == false)
                return;

            NetworkSceneAsyncOp operation;
            try
            {
                switch (desiredMode)
                {
                    case SceneSwitchMode.Load:
                        operation = RunnerAdditiveSceneManager.LoadAdditiveScene(runner, sceneRef, _localPhysicsMode, _setActiveOnLoad);
                        break;

                    case SceneSwitchMode.Unload:
                        operation = RunnerAdditiveSceneManager.UnloadAdditiveScene(runner, sceneRef);
                        break;

                    case SceneSwitchMode.Toggle:
                        operation = RunnerAdditiveSceneManager.LoadAdditiveScene(runner, sceneRef, _localPhysicsMode, _setActiveOnLoad);
                        break;

                    default:
                        Debug.LogWarning($"{nameof(SceneSwitcher)} on {name} has an unsupported mode {desiredMode}.", this);
                        return;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(SceneSwitcher)} on {name} encountered an error while switching scenes: {exception.Message}", this);
                return;
            }

            _switchRequested = true;

            if (operation.IsValid == true)
            {
                operation.AddOnCompleted(_ => _switchRequested = false);
            }
            else
            {
                _switchRequested = false;
            }
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

        private enum SceneSwitchMode
        {
            Load,
            Unload,
            Toggle,
        }
    }
}
