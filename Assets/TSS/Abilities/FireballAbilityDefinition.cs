using UnityEngine;

namespace TPSBR.Abilities
{
    [CreateAssetMenu(fileName = "FireballAbilityDefinition", menuName = "TSS/Abilities/Fireball Ability")]
    public class FireballAbilityDefinition : StaffAbilityDefinition
    {
        public const string AbilityCode = "FIREBALL";

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetStringCode(AbilityCode);
        }
#endif
    }
}
