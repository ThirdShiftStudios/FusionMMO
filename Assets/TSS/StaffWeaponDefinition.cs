using System.Collections.Generic;
using TPSBR.Abilities;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class StaffWeaponDefinition : WeaponDefinition
    {
        private const string DefaultFireballAbilityResource = "Abilities/FireballAbilityDefinition";
        private const string AbilityResourceFolder = "Abilities";

        private static StaffAbilityDefinition[] _cachedDefaultAbilities;
        private static readonly List<StaffAbilityDefinition> _abilityCache = new List<StaffAbilityDefinition>();

        private void OnEnable()
        {
            EnsureDefaultAbilities();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            EnsureDefaultAbilities();
        }
#endif

        private void EnsureDefaultAbilities()
        {
            if (HasConfiguredAbilities() == true)
            {
                return;
            }

            StaffAbilityDefinition[] defaultAbilities = GetDefaultAbilityDefinitions();

            if (defaultAbilities == null || defaultAbilities.Length == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"Unable to locate any staff ability definitions. Checked Resources/{AbilityResourceFolder} and the root Resources folder.");
#endif
                return;
            }

            SetAvailableAbilities(defaultAbilities);
        }

        private bool HasConfiguredAbilities()
        {
            var abilities = AvailableAbilities;

            if (abilities == null)
            {
                return false;
            }

            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static StaffAbilityDefinition[] GetDefaultAbilityDefinitions()
        {
            if (_cachedDefaultAbilities != null && _cachedDefaultAbilities.Length > 0)
            {
                return _cachedDefaultAbilities;
            }

            _abilityCache.Clear();
            AppendAbilityAssets(_abilityCache, Resources.LoadAll<StaffAbilityDefinition>(AbilityResourceFolder));
            AppendAbilityAssets(_abilityCache, Resources.LoadAll<StaffAbilityDefinition>(string.Empty));

            if (_abilityCache.Count == 0)
            {
                FireballAbilityDefinition fallback = Resources.Load<FireballAbilityDefinition>(DefaultFireballAbilityResource);

                if (fallback != null)
                {
                    _abilityCache.Add(fallback);
                }
            }

            _cachedDefaultAbilities = _abilityCache.ToArray();
            return _cachedDefaultAbilities;
        }

        private static void AppendAbilityAssets(List<StaffAbilityDefinition> destination, StaffAbilityDefinition[] source)
        {
            if (source == null || source.Length == 0)
            {
                return;
            }

            for (int i = 0; i < source.Length; i++)
            {
                StaffAbilityDefinition ability = source[i];

                if (ability == null)
                {
                    continue;
                }

                if (destination.Contains(ability) == true)
                {
                    continue;
                }

                destination.Add(ability);
            }
        }
    }
}
