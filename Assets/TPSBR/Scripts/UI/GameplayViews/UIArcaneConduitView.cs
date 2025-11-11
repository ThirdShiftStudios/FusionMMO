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

        internal void SetConduit(ArcaneConduit conduit)
        {
            _ = conduit;
        }

        internal void SetAbilityOptions(IReadOnlyList<ArcaneConduit.AbilityOption> options)
        {
            base.SetAbilityOptions(options);
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
    }
}
