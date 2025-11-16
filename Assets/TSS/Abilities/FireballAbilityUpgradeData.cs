using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public sealed class FireballAbilityUpgradeData : AbilityUpgradeData
    {
        private const string RadiusTrackId = "RadiusDelta";
        private const string DamageTrackId = "DamageDelta";
        private const string CastTimeTrackId = "CastingTimeDelta";

        [SerializeField]
        private float[] _radiusDeltaPerLevel = Array.Empty<float>();

        [SerializeField]
        private float[] _damageDeltaPerLevel = Array.Empty<float>();

        [SerializeField]
        private float[] _castingTimeDeltaPerLevel = Array.Empty<float>();

        public IReadOnlyList<float> RadiusDeltaPerLevel => _radiusDeltaPerLevel ?? Array.Empty<float>();
        public IReadOnlyList<float> DamageDeltaPerLevel => _damageDeltaPerLevel ?? Array.Empty<float>();
        public IReadOnlyList<float> CastingTimeDeltaPerLevel => _castingTimeDeltaPerLevel ?? Array.Empty<float>();

        public override IEnumerable<AbilityUpgradeTrack> EnumerateTracks()
        {
            yield return new AbilityUpgradeTrack(RadiusTrackId, "Radius", RadiusDeltaPerLevel);
            yield return new AbilityUpgradeTrack(DamageTrackId, "Damage", DamageDeltaPerLevel);
            yield return new AbilityUpgradeTrack(CastTimeTrackId, "Cast Time", CastingTimeDeltaPerLevel);
        }
    }
}
