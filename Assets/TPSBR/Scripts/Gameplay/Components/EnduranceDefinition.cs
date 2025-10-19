using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "EnduranceDefinition", menuName = "TSS/Stats/Endurance")]
    public sealed class EnduranceDefinition : StatDefinition
    {
        public override float GetTotalHealth(int statLevel)
        {
            return Mathf.Max(0, statLevel) * 20f;
        }
    }
}