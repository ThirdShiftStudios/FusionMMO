using DungeonArchitect;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPSBR
{
    public class WalkInDungeonEntrance : SceneSwitcher
    {
        private void OnEnable()
        {
            OnSceneFinishedLoading += HandleSceneFinishedLoading;
        }

        private void OnDisable()
        {
            OnSceneFinishedLoading -= HandleSceneFinishedLoading;
        }

        private void HandleSceneFinishedLoading(Scene loadedScene)
        {
            if (loadedScene.IsValid() == false || loadedScene.isLoaded == false)
            {
                Debug.LogWarning($"{nameof(WalkInDungeonEntrance)} received an invalid scene notification.", this);
                return;
            }

            var dungeonManager = FindDungeonManager(loadedScene);
            if (dungeonManager == null)
            {
                Debug.LogWarning($"{nameof(WalkInDungeonEntrance)} could not locate a {nameof(NetworkedDungeonManager)} in scene '{loadedScene.name}'.", this);
                return;
            }

            if (dungeonManager.HasStateAuthority == true)
            {
                dungeonManager.RandomizeDungeonSeed();
            }

            dungeonManager.GenerateDungeonIfReady();
        }

        private static NetworkedDungeonManager FindDungeonManager(Scene scene)
        {
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                var manager = rootObject.GetComponentInChildren<NetworkedDungeonManager>(true);
                if (manager != null)
                {
                    return manager;
                }
            }

            return null;
        }
    }
}
