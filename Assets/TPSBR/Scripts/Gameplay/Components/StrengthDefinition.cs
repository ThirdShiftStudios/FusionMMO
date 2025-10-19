using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "StrengthDefinition", menuName = "TSS/Stats/Strength")]
    public sealed class StrengthDefinition : StatDefinition
    {
        public override float GetTotalHealth(int statLevel)
        {
            return Mathf.Max(0, statLevel) * 100f;
        }
    }
}