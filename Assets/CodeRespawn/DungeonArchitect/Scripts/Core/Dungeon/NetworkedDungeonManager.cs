using Fusion;
using UnityEngine;

namespace DungeonArchitect
{
    [RequireComponent(typeof(Dungeon))]
    public sealed class NetworkedDungeonManager : NetworkBehaviour
    {
        [Networked]
        private int _seed { get; set; }

        [Networked]
        private NetworkBool _dungeonReadyToGenerate { get; set; }

        private Dungeon _dungeon;
        private int _lastSeedGenerated = -1;

        private void Awake()
        {
            CacheDungeonComponent();
        }

        public override void Spawned()
        {
            base.Spawned();
            CacheDungeonComponent();
            TryGenerateDungeon();
        }

        public override void FixedUpdateNetwork()
        {
            TryGenerateDungeon();
        }

        public override void Render()
        {
            TryGenerateDungeon();
        }

        public void RandomizeDungeonSeed()
        {
            if (HasStateAuthority == false)
            {
                Debug.LogWarning("RandomizeDungeonSeed can only be called by the state authority.");
                return;
            }

            CacheDungeonComponent();
            if (_dungeon == null)
            {
                Debug.LogWarning("NetworkedDungeonManager requires a Dungeon component on the same GameObject.");
                return;
            }

            _dungeon.RandomizeSeed();
            _seed = (int)_dungeon.Config.Seed;
            _dungeonReadyToGenerate = true;
            _lastSeedGenerated = -1;
            TryGenerateDungeon();
        }

        public void SynchronizeSeed(int seed)
        {
            if (HasStateAuthority == false)
            {
                Debug.LogWarning("SynchronizeSeed can only be called by the state authority.");
                return;
            }

            CacheDungeonComponent();
            if (_dungeon == null)
            {
                Debug.LogWarning("NetworkedDungeonManager requires a Dungeon component on the same GameObject.");
                return;
            }

            _dungeon.SetSeed(seed);
            _seed = seed;
            _dungeonReadyToGenerate = true;
            _lastSeedGenerated = -1;
            TryGenerateDungeon();
        }

        public void SetDungeonReadyToGenerate(bool ready)
        {
            if (HasStateAuthority == false)
            {
                Debug.LogWarning("SetDungeonReadyToGenerate can only be called by the state authority.");
                return;
            }

            _dungeonReadyToGenerate = ready;
        }

        private void TryGenerateDungeon()
        {
            if (_dungeonReadyToGenerate == false)
            {
                return;
            }

            if (_seed == _lastSeedGenerated)
            {
                return;
            }

            CacheDungeonComponent();
            if (_dungeon == null)
            {
                Debug.LogWarning("NetworkedDungeonManager requires a Dungeon component on the same GameObject.");
                return;
            }

            _dungeon.SetSeed(_seed);
            _dungeon.Build();
            _lastSeedGenerated = _seed;
        }

        private void CacheDungeonComponent()
        {
            if (_dungeon == null)
            {
                _dungeon = GetComponent<Dungeon>();
            }
        }
    }
}
