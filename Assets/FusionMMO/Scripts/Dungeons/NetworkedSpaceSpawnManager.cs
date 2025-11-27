using System.Collections.Generic;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public static class NetworkedSpaceSpawnManager
    {
        private const float MIN_DISTANCE = 1000f;
        private static readonly List<Vector3> _occupiedPositions = new List<Vector3>();

        public static Vector3 AllocatePosition()
        {
            Vector3 basePosition = new Vector3(MIN_DISTANCE, MIN_DISTANCE, MIN_DISTANCE);
            if (_occupiedPositions.Count == 0)
            {
                _occupiedPositions.Add(basePosition);
                return basePosition;
            }

            int attempts = 0;
            while (attempts < 1024)
            {
                attempts++;
                Vector3 candidate = basePosition + new Vector3(MIN_DISTANCE * attempts, 0f, 0f);
                if (IsPositionAvailable(candidate))
                {
                    _occupiedPositions.Add(candidate);
                    return candidate;
                }
            }

            Vector3 fallback = basePosition + new Vector3(MIN_DISTANCE * (attempts + 1), 0f, 0f);
            _occupiedPositions.Add(fallback);
            Debug.LogWarning($"{nameof(NetworkedSpaceSpawnManager)} failed to find a widely spaced position. Using fallback at {fallback}.");
            return fallback;
        }

        private static bool IsPositionAvailable(Vector3 candidate)
        {
            for (int i = 0; i < _occupiedPositions.Count; ++i)
            {
                float distance = Vector3.Distance(_occupiedPositions[i], candidate);
                if (distance < MIN_DISTANCE)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
