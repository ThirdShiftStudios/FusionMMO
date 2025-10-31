using System;
using System.Collections.Generic;

namespace TPSBR.UI
{
    public sealed class UIArcaneConduitView : UIItemContextView
    {
        private readonly List<ArcaneConduit.AbilityOption> _abilityOptions = new List<ArcaneConduit.AbilityOption>();

        public event Action<int> AbilityPurchaseRequested;

        internal void SetConduit(ArcaneConduit conduit)
        {
            _ = conduit;
        }

        internal void SetAbilityOptions(IReadOnlyList<ArcaneConduit.AbilityOption> options)
        {
            _abilityOptions.Clear();

            if (options != null)
            {
                _abilityOptions.AddRange(options);
            }

            // Concrete UI updates are handled within the Unity editor setup.
        }

        internal void ClearAbilityOptions()
        {
            _abilityOptions.Clear();
            // Concrete UI updates are handled within the Unity editor setup.
        }

        public IReadOnlyList<ArcaneConduit.AbilityOption> AbilityOptions => _abilityOptions;

        public void RequestAbilityPurchase(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= _abilityOptions.Count)
                return;

            AbilityPurchaseRequested?.Invoke(_abilityOptions[optionIndex].Index);
        }

        public void RequestAbilityPurchaseByAbilityIndex(int abilityIndex)
        {
            AbilityPurchaseRequested?.Invoke(abilityIndex);
        }
    }
}
