namespace TPSBR
{
    using System;
    using System.Collections.Generic;
    using Fusion;
    using UnityEngine;

    public sealed class MountCollection : ContextBehaviour
    {
        [SerializeField] private MountDefinition[] _initialMounts;
        [SerializeField] private MountDefinition _equippedMount;

        private readonly List<string> _ownedMountCodes = new List<string>();

        public IReadOnlyList<string> OwnedMountCodes => _ownedMountCodes;
        public string ActiveMountCode { get; private set; }

        public event Action MountsChanged;

        public override void Spawned()
        {
            base.Spawned();

            if (_initialMounts != null)
            {
                for (int i = 0; i < _initialMounts.Length; i++)
                {
                    var definition = _initialMounts[i];
                    string mountCode = definition != null ? definition.Identifier : null;
                    if (mountCode.HasValue() == false)
                        continue;

                    Unlock(mountCode, false);
                }
            }

            if (_equippedMount != null && _equippedMount.Identifier.HasValue() == true)
            {
                SetActiveMount(_equippedMount.Identifier);
            }

            Global.PlayerCloudSaveService?.RegisterMountCollectionAndRestore(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Global.PlayerCloudSaveService?.UnregisterMountCollection(this);

            base.Despawned(runner, hasState);
        }

        public bool HasMount(string mountCode)
        {
            if (mountCode.HasValue() == false)
                return false;

            return _ownedMountCodes.Contains(mountCode);
        }

        public bool Unlock(string mountCode, bool notify = true)
        {
            if (mountCode.HasValue() == false)
                return false;

            if (_ownedMountCodes.Contains(mountCode) == true)
                return false;

            _ownedMountCodes.Add(mountCode);

            if (notify == true)
            {
                MountsChanged?.Invoke();
            }

            return true;
        }

        public bool SetActiveMount(string mountCode)
        {
            if (mountCode.HasValue() == false)
            {
                ActiveMountCode = null;
                MountsChanged?.Invoke();
                return true;
            }

            if (HasMount(mountCode) == false)
                return false;

            if (ActiveMountCode == mountCode)
                return false;

            ActiveMountCode = mountCode;
            MountsChanged?.Invoke();
            return true;
        }

        public PlayerCharacterMountSaveData CreateSaveData(string characterId)
        {
            if (characterId.HasValue() == false)
                return null;

            var snapshot = new PlayerCharacterMountSaveData
            {
                CharacterId = characterId,
                ActiveMountCode = ActiveMountCode,
                OwnedMounts = _ownedMountCodes.ToArray(),
            };

            return snapshot;
        }

        public void ApplySaveData(PlayerCharacterMountSaveData data)
        {
            if (data == null)
                return;

            _ownedMountCodes.Clear();

            if (data.OwnedMounts != null)
            {
                for (int i = 0; i < data.OwnedMounts.Length; i++)
                {
                    Unlock(data.OwnedMounts[i], false);
                }
            }

            ActiveMountCode = data.ActiveMountCode;
            MountsChanged?.Invoke();
        }
    }
}
