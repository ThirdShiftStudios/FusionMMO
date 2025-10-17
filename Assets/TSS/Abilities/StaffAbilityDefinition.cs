using TPSBR;

namespace TPSBR.Abilities
{
    public interface IStaffAbilityHandler
    {
        void Execute(StaffWeapon staffWeapon);
    }

    public abstract class StaffAbilityDefinition : AbilityDefinition, IStaffAbilityHandler
    {
        public abstract void Execute(StaffWeapon staffWeapon);
    }
}
