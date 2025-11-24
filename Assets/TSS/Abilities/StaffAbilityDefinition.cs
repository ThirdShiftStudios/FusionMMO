using TPSBR;
using UnityEngine;

namespace TPSBR.Abilities
{
    public interface IStaffAbilityHandler
    {
        void Execute(StaffWeapon staffWeapon);
    }

    public abstract class StaffAbilityDefinition : AbilityDefinition, IStaffAbilityHandler, IAppliesBuff
    {
        [SerializeField]
        private BuffDefinition _buffDefinition;

        public BuffDefinition BuffDefinition => _buffDefinition;

        public abstract void Execute(StaffWeapon staffWeapon);
    }
}
