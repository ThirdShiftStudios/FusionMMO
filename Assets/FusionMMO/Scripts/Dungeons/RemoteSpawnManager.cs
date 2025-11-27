using System.Collections.Generic;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public static class RemoteSpawnManager
    {
        private const float SPAWN_SPACING = 1000f;
        private static readonly Vector3 _basePosition = new Vector3(SPAWN_SPACING, SPAWN_SPACING, SPAWN_SPACING);
        private static readonly Dictionary<RemoteEntranceBase, Vector3> _reservedPositions = new Dictionary<RemoteEntranceBase, Vector3>();
        private static int _reservedCount;

        public static Vector3 GetOrReservePosition(RemoteEntranceBase entrance)
        {
            if (entrance == null)
            {
                return _basePosition;
            }

            if (_reservedPositions.TryGetValue(entrance, out var position))
            {
                return position;
            }

            position = _basePosition + new Vector3(SPAWN_SPACING * _reservedCount, 0f, 0f);
            _reservedPositions.Add(entrance, position);
            _reservedCount++;

            return position;
        }

        public static void Release(RemoteEntranceBase entrance)
        {
            if (entrance == null)
            {
                return;
            }

            _reservedPositions.Remove(entrance);
        }
    }
}
