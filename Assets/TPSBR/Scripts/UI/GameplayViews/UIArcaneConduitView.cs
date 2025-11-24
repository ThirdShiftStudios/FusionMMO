using System;
using System.Collections.Generic;

using StaffWeapon = TPSBR.StaffWeapon;

namespace TPSBR.UI
{
    public sealed class UIArcaneConduitView : UIItemContextView
    {
        public event Action<int> AbilityPurchaseRequested
        {
            add => AbilityUnlockRequested += value;
            remove => AbilityUnlockRequested -= value;
        }

        public event Action<StaffWeapon.AbilityControlSlot, int> AbilityAssignmentRequested
        {
            add => base.AbilityAssignmentRequested += value;
            remove => base.AbilityAssignmentRequested -= value;
        }

        public event Action<int> AbilityLevelUpRequested
        {
            add => base.AbilityLevelUpRequested += value;
            remove => base.AbilityLevelUpRequested -= value;
        }

        internal void SetConduit(ArcaneConduit conduit)
        {
            _ = conduit;
        }

        internal void SetAbilityOptions(IReadOnlyList<ArcaneConduit.AbilityOption> options, string configurationHash)
        {
            base.SetAbilityOptions(options, configurationHash);
        }

        internal void SetAbilityAssignments(IReadOnlyList<int> assignments)
        {
            base.SetAbilityAssignments(assignments);
        }

        internal void ClearAbilityOptions()
        {
            base.ClearAbilityOptions();
        }

        internal void ClearAbilityAssignments()
        {
            base.ClearAbilityAssignments();
        }

        public IReadOnlyList<ArcaneConduit.AbilityOption> AbilityOptions => base.GetAbilityOptions();

        public void RequestAbilityPurchase(int optionIndex)
        {
            base.RequestAbilityUnlockByOptionIndex(optionIndex);
        }

        public void RequestAbilityPurchaseByAbilityIndex(int abilityIndex)
        {
            base.RequestAbilityUnlockByAbilityIndex(abilityIndex);
        }

        public void RequestAbilityLevelUp(int abilityIndex)
        {
            base.RequestAbilityLevelUpByAbilityIndex(abilityIndex);
        }
    }
}
