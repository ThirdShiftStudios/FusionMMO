using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine.SceneManagement;

namespace TPSBR
{
    /// <summary>
    /// Helper responsible for managing additive scene loads on a per-runner basis.
    /// Provides event hooks that gameplay systems can subscribe to in order to react to scene transitions.
    /// </summary>
    public static class RunnerAdditiveSceneManager
    {
        public static event Action<NetworkRunner, SceneRef> SceneLoadStarted;
        public static event Action<NetworkRunner, SceneRef> SceneLoadCompleted;
        public static event Action<NetworkRunner, SceneRef, Exception> SceneLoadFailed;
        public static event Action<NetworkRunner, SceneRef> SceneUnloadStarted;
        public static event Action<NetworkRunner, SceneRef> SceneUnloadCompleted;

        private enum SceneOperationState
        {
            Loading,
            Loaded,
            Unloading,
        }

        private class SceneEntry
        {
            public NetworkSceneLoadId LoadId;
            public NetworkSceneAsyncOp Operation;
            public SceneOperationState State;
        }

        private class RunnerEntry
        {
            public readonly Dictionary<SceneRef, SceneEntry> Scenes = new Dictionary<SceneRef, SceneEntry>();
        }

        private static readonly Dictionary<NetworkRunner, RunnerEntry> _runners = new Dictionary<NetworkRunner, RunnerEntry>();

        /// <summary>
        /// Returns true when the specified scene has finished loading additively on the provided runner.
        /// </summary>
        public static bool IsSceneLoaded(NetworkRunner runner, SceneRef sceneRef)
        {
            if (TryGetSceneEntry(runner, sceneRef, out var entry) == false)
                return false;

            return entry.State == SceneOperationState.Loaded;
        }

        /// <summary>
        /// Returns true when the specified scene is currently being loaded or unloaded.
        /// </summary>
        public static bool IsSceneOperationInProgress(NetworkRunner runner, SceneRef sceneRef)
        {
            if (TryGetSceneEntry(runner, sceneRef, out var entry) == false)
                return false;

            return entry.State == SceneOperationState.Loading || entry.State == SceneOperationState.Unloading;
        }

        /// <summary>
        /// Attempts to resolve a SceneRef from the provided runner and scene path.
        /// </summary>
        public static bool TryResolveSceneRef(NetworkRunner runner, string scenePath, out SceneRef sceneRef)
        {
            sceneRef = SceneRef.None;

            if (runner == null || string.IsNullOrEmpty(scenePath) == true)
                return false;

            var sceneManager = runner.SceneManager as NetworkSceneManagerDefault;
            if (sceneManager != null)
            {
                sceneRef = sceneManager.GetSceneRef(scenePath);
                if (sceneRef.IsValid == true)
                    return true;

                string assetPath = ToAssetPath(scenePath);
                sceneRef = sceneManager.GetSceneRef(assetPath);
                if (sceneRef.IsValid == true)
                    return true;
            }

            string fullPath = ToAssetPath(scenePath);
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(fullPath);
            if (buildIndex >= 0)
            {
                sceneRef = SceneRef.FromIndex(buildIndex);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Initiates an additive scene load for the provided scene reference.
        /// </summary>
        public static NetworkSceneAsyncOp LoadAdditiveScene(NetworkRunner runner, SceneRef sceneRef, LocalPhysicsMode physicsMode, bool setActiveOnLoad)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));

            if (sceneRef.IsValid == false)
                throw new ArgumentException("The provided sceneRef is not valid.", nameof(sceneRef));

            var sceneManager = runner.SceneManager as NetworkSceneManagerDefault;
            if (sceneManager == null)
                throw new InvalidOperationException("Runner does not use NetworkSceneManagerDefault; additive scene loading is not supported.");

            if (TryGetSceneEntry(runner, sceneRef, out var existing) == true)
            {
                if (existing.State == SceneOperationState.Loaded)
                    return existing.Operation;

                if (existing.State == SceneOperationState.Loading)
                    return existing.Operation;

                if (existing.State == SceneOperationState.Unloading)
                    throw new InvalidOperationException("Cannot load a scene that is currently unloading.");
            }

            var parameters = new NetworkLoadSceneParameters
            {
                LoadSceneMode = LoadSceneMode.Additive,
                LocalPhysicsMode = physicsMode,
                LoadSceneParameters = new LoadSceneParameters(LoadSceneMode.Additive, physicsMode),
                LoadId = NetworkSceneLoadId.New(),
                IsActiveOnLoad = setActiveOnLoad,
                IsSingleLoad = false,
            };

            var asyncOp = sceneManager.LoadScene(sceneRef, parameters);

            var entry = RegisterSceneEntry(runner, sceneRef);
            entry.LoadId = parameters.LoadId;
            entry.Operation = asyncOp;
            entry.State = SceneOperationState.Loading;

            SceneLoadStarted?.Invoke(runner, sceneRef);

            if (asyncOp.IsValid)
            {
                asyncOp.AddOnCompleted(operation =>
                {
                    if (operation.Error != null)
                    {
                        RemoveSceneEntry(runner, sceneRef);
                        SceneLoadFailed?.Invoke(runner, sceneRef, operation.Error);
                        return;
                    }

                    if (TryGetSceneEntry(runner, sceneRef, out var current) == true)
                    {
                        current.State = SceneOperationState.Loaded;
                    }

                    SceneLoadCompleted?.Invoke(runner, sceneRef);
                });
            }
            else
            {
                RemoveSceneEntry(runner, sceneRef);
                SceneLoadFailed?.Invoke(runner, sceneRef, new InvalidOperationException("The additive scene load operation is not valid."));
            }

            return asyncOp;
        }

        /// <summary>
        /// Initiates unloading of an additive scene.
        /// </summary>
        public static NetworkSceneAsyncOp UnloadAdditiveScene(NetworkRunner runner, SceneRef sceneRef)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));

            if (sceneRef.IsValid == false)
                throw new ArgumentException("The provided sceneRef is not valid.", nameof(sceneRef));

            var sceneManager = runner.SceneManager as NetworkSceneManagerDefault;
            if (sceneManager == null)
                throw new InvalidOperationException("Runner does not use NetworkSceneManagerDefault; additive scene unloading is not supported.");

            if (TryGetSceneEntry(runner, sceneRef, out var entry) == false)
                throw new InvalidOperationException("Cannot unload a scene that is not tracked as loaded or loading.");

            if (entry.State == SceneOperationState.Loading)
                throw new InvalidOperationException("Cannot unload a scene that is currently loading.");

            if (entry.State == SceneOperationState.Unloading)
                return entry.Operation;

            var asyncOp = sceneManager.UnloadScene(sceneRef);

            entry.Operation = asyncOp;
            entry.State = SceneOperationState.Unloading;

            SceneUnloadStarted?.Invoke(runner, sceneRef);

            if (asyncOp.IsValid)
            {
                asyncOp.AddOnCompleted(_ =>
                {
                    RemoveSceneEntry(runner, sceneRef);
                    SceneUnloadCompleted?.Invoke(runner, sceneRef);
                });
            }
            else
            {
                RemoveSceneEntry(runner, sceneRef);
                SceneUnloadCompleted?.Invoke(runner, sceneRef);
            }

            return asyncOp;
        }

        /// <summary>
        /// Clears all tracking data for the provided runner. Should be invoked when the runner is shut down.
        /// </summary>
        public static void ClearRunner(NetworkRunner runner)
        {
            if (runner == null)
                return;

            _runners.Remove(runner);
        }

        private static RunnerEntry GetOrCreateRunnerEntry(NetworkRunner runner)
        {
            if (_runners.TryGetValue(runner, out var entry) == false)
            {
                entry = new RunnerEntry();
                _runners.Add(runner, entry);
            }

            return entry;
        }

        private static SceneEntry RegisterSceneEntry(NetworkRunner runner, SceneRef sceneRef)
        {
            var runnerEntry = GetOrCreateRunnerEntry(runner);

            if (runnerEntry.Scenes.TryGetValue(sceneRef, out var entry) == false)
            {
                entry = new SceneEntry();
                runnerEntry.Scenes.Add(sceneRef, entry);
            }

            return entry;
        }

        private static void RemoveSceneEntry(NetworkRunner runner, SceneRef sceneRef)
        {
            if (_runners.TryGetValue(runner, out var runnerEntry) == false)
                return;

            runnerEntry.Scenes.Remove(sceneRef);

            if (runnerEntry.Scenes.Count == 0)
            {
                _runners.Remove(runner);
            }
        }

        private static bool TryGetSceneEntry(NetworkRunner runner, SceneRef sceneRef, out SceneEntry entry)
        {
            entry = null;

            if (runner == null)
                return false;

            if (_runners.TryGetValue(runner, out var runnerEntry) == false)
                return false;

            return runnerEntry.Scenes.TryGetValue(sceneRef, out entry);
        }

        private static string ToAssetPath(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath) == true)
                return scenePath;

            bool hasAssetsPrefix = scenePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
            bool hasUnitySuffix = scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);

            if (hasAssetsPrefix == false)
            {
                scenePath = $"Assets/{scenePath}";
            }

            if (hasUnitySuffix == false)
            {
                scenePath = $"{scenePath}.unity";
            }

            return scenePath;
        }
    }
}

