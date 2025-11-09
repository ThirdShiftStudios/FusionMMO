using DungeonArchitect;
using UnityEngine;

namespace TPSBR
{
    public class WalkInDungeonEntrance : SceneSwitcher
    {
        private NetworkedDungeonManager _dungeonManager;

        private void OnEnable()
        {
            OnSceneFinishedLoading += HandleSceneFinishedLoading;
        }

        private void OnDisable()
        {
            OnSceneFinishedLoading -= HandleSceneFinishedLoading;
        }

        private void HandleSceneFinishedLoading()
        {
            if (_dungeonManager == null)
            {
                _dungeonManager = GetComponent<NetworkedDungeonManager>();
            }

            if (_dungeonManager == null)
            {
                Debug.LogWarning($"{nameof(WalkInDungeonEntrance)} on {name} requires a {nameof(NetworkedDungeonManager)} component on the same GameObject.", this);
                return;
            }

            if (_dungeonManager.HasStateAuthority == true)
            {
                _dungeonManager.RandomizeDungeonSeed();
            }

            _dungeonManager.GenerateDungeonIfReady();
        }
    }
}
