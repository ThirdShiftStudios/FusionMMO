using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "DexterityDefinition", menuName = "TSS/Stats/Dexterity")]
    public sealed class DexterityDefinition : StatDefinition
    {
        public override float GetMovementSpeedMultiplier(int statLevel)
        {
            return Mathf.Max(0, statLevel) * 0.01f;
        }
    }
}