using System;
using System.Collections.Generic;

namespace TPSBR.UI
{
    public sealed class UIArcaneConduitView : UIItemContextView
    {
        public event Action<int> AbilityPurchaseRequested
        {
            add => AbilityUnlockRequested += value;
            remove => AbilityUnlockRequested -= value;
        }

        public event Action<IReadOnlyList<int>> AbilityControlBindingsChanged
        {
            add => base.AbilityControlBindingsChanged += value;
            remove => base.AbilityControlBindingsChanged -= value;
        }

        internal void SetConduit(ArcaneConduit conduit)
        {
            _ = conduit;
        }

        internal void SetAbilityOptions(IReadOnlyList<ArcaneConduit.AbilityOption> options)
        {
            base.SetAbilityOptions(options);
        }

        internal void ClearAbilityOptions()
        {
            base.ClearAbilityOptions();
        }

        internal void SetAbilityControlBindings(IReadOnlyList<int> bindings)
        {
            base.SetAbilityControlBindings(bindings);
        }

        internal void ClearAbilityControlBindings()
        {
            base.ClearAbilityControlBindings();
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
