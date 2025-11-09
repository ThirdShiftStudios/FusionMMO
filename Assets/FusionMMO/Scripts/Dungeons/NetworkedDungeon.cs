using System;
using DungeonArchitect;
using Fusion;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    [RequireComponent(typeof(Dungeon))]
    public class NetworkedDungeon : NetworkBehaviour
    {
        [Networked]
        private int _seed { get; set; }

        [Networked]
        private NetworkBool _dungeonReadyToGenerate { get; set; }

        private int _lastSeedGenerated = -1;
        private Dungeon _dungeon;

        public event Action<NetworkedDungeon> DungeonGenerated;

        public bool IsGenerated => _dungeonReadyToGenerate && _lastSeedGenerated == _seed;

        private void Awake()
        {
            CacheDungeon();
        }

        public override void Spawned()
        {
            base.Spawned();
            CacheDungeon();
            TryGenerateDungeon();
        }

        public override void Render()
        {
            base.Render();
            TryGenerateDungeon();
        }

        public void RandomizeSeed()
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (CacheDungeon() == false)
            {
                Debug.LogWarning($"{nameof(NetworkedDungeon)} requires a {nameof(Dungeon)} component on the same GameObject.", this);
                return;
            }

            _dungeon.RandomizeSeed();

            var config = _dungeon.Config;
            if (config != null)
            {
                _seed = (int)config.Seed;
            }

            _dungeonReadyToGenerate = true;
            TryGenerateDungeon();
        }

        private bool CacheDungeon()
        {
            if (_dungeon != null)
            {
                return true;
            }

            _dungeon = GetComponent<Dungeon>();
            return _dungeon != null;
        }

        private void TryGenerateDungeon()
        {
            if (_dungeonReadyToGenerate == false)
            {
                return;
            }

            if (CacheDungeon() == false)
            {
                return;
            }

            if (_lastSeedGenerated == _seed)
            {
                return;
            }

            _dungeon.SetSeed(_seed);
            _dungeon.Build();

            _lastSeedGenerated = _seed;

            NotifyDungeonGenerated();
        }

        private void NotifyDungeonGenerated()
        {
            DungeonGenerated?.Invoke(this);
        }
    }
}
