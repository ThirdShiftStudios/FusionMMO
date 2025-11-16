using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public sealed class FireNovaAbilityUpgradeData : AbilityUpgradeData
    {
        private const string RadiusTrackId = "RadiusDelta";
        private const string DamageTrackId = "DamageDelta";
        private const string BurnDurationTrackId = "BurnDurationDelta";
        private const string BurnDamageTrackId = "BurnDamageDelta";
        private const string CastTimeTrackId = "CastingTimeDelta";

        [SerializeField]
        private float[] _radiusDeltaPerLevel = Array.Empty<float>();

        [SerializeField]
        private float[] _damageDeltaPerLevel = Array.Empty<float>();

        [SerializeField]
        private float[] _burnDurationDeltaPerLevel = Array.Empty<float>();

        [SerializeField]
        private float[] _burnDamageDeltaPerLevel = Array.Empty<float>();

        [SerializeField]
        private float[] _castingTimeDeltaPerLevel = Array.Empty<float>();

        public IReadOnlyList<float> RadiusDeltaPerLevel => _radiusDeltaPerLevel ?? Array.Empty<float>();
        public IReadOnlyList<float> DamageDeltaPerLevel => _damageDeltaPerLevel ?? Array.Empty<float>();
        public IReadOnlyList<float> BurnDurationDeltaPerLevel => _burnDurationDeltaPerLevel ?? Array.Empty<float>();
        public IReadOnlyList<float> BurnDamageDeltaPerLevel => _burnDamageDeltaPerLevel ?? Array.Empty<float>();
        public IReadOnlyList<float> CastingTimeDeltaPerLevel => _castingTimeDeltaPerLevel ?? Array.Empty<float>();

        public override IEnumerable<AbilityUpgradeTrack> EnumerateTracks()
        {
            yield return new AbilityUpgradeTrack(RadiusTrackId, "Radius", RadiusDeltaPerLevel);
            yield return new AbilityUpgradeTrack(DamageTrackId, "Damage", DamageDeltaPerLevel);
            yield return new AbilityUpgradeTrack(BurnDurationTrackId, "Burn Duration", BurnDurationDeltaPerLevel);
            yield return new AbilityUpgradeTrack(BurnDamageTrackId, "Burn Damage", BurnDamageDeltaPerLevel);
            yield return new AbilityUpgradeTrack(CastTimeTrackId, "Cast Time", CastingTimeDeltaPerLevel);
        }
    }
}
