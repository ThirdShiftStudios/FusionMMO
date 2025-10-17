using TPSBR.Abilities;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class StaffWeaponDefinition : WeaponDefinition
    {
        private const string DefaultFireballAbilityResource = "Abilities/FireballAbilityDefinition";

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
            var abilities = AvailableAbilities;

            if (abilities != null)
            {
                for (int i = 0; i < abilities.Count; i++)
                {
                    if (abilities[i] != null)
                    {
                        return;
                    }
                }
            }

            FireballAbilityDefinition fireball = Resources.Load<FireballAbilityDefinition>(DefaultFireballAbilityResource);

            if (fireball == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"Unable to locate default fireball ability definition at Resources/{DefaultFireballAbilityResource}.");
#endif
                return;
            }

            SetAvailableAbilities(fireball);
        }
    }
}
