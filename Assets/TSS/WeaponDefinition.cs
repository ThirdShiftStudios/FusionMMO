using System;
using System.Collections.Generic;
using TPSBR;
using TPSBR.Abilities;
using TSS.Data;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class WeaponDefinition : ItemDefinition
    {
        [SerializeField]
        private Weapon _weaponPrefab;
        [SerializeField]
        private AbilityDefinition[] _availableAbilities = Array.Empty<AbilityDefinition>();

        public Weapon WeaponPrefab => _weaponPrefab;
        public IReadOnlyList<AbilityDefinition> AvailableAbilities => _availableAbilities ?? Array.Empty<AbilityDefinition>();
        protected ushort ItemDefinitionMaxStack => base.MaxStack;
        public override ushort MaxStack => 1;
        public override ESlotCategory SlotCategory => ESlotCategory.Weapon;

        public bool HasAbility(AbilityDefinition ability)
        {
            if (ability == null || _availableAbilities == null)
            {
                return false;
            }

            return Array.IndexOf(_availableAbilities, ability) >= 0;
        }

        public bool TryGetAbility(string stringCode, out AbilityDefinition ability)
        {
            ability = null;

            if (string.IsNullOrWhiteSpace(stringCode) == true || _availableAbilities == null)
            {
                return false;
            }

            for (int i = 0; i < _availableAbilities.Length; i++)
            {
                AbilityDefinition candidate = _availableAbilities[i];

                if (candidate == null)
                {
                    continue;
                }

                if (candidate.IsStringCode(stringCode, StringComparison.OrdinalIgnoreCase) == true)
                {
                    ability = candidate;
                    return true;
                }
            }

            return false;
        }

        protected void SetAvailableAbilities(params AbilityDefinition[] abilities)
        {
            if (abilities == null || abilities.Length == 0)
            {
                _availableAbilities = Array.Empty<AbilityDefinition>();
                return;
            }

            _availableAbilities = new AbilityDefinition[abilities.Length];
            Array.Copy(abilities, _availableAbilities, abilities.Length);
        }
    }
}
