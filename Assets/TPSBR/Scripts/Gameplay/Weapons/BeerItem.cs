using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public sealed class BeerItem : Weapon
    {
        [SerializeField]
        [Tooltip("Optional override for the amount healed when the beer is consumed. If left at 0 the definition value is used.")]
        private float _overrideHealAmount;

        private void Awake()
        {
            SetWeaponSize(WeaponSize.Consumable);
        }

        private BeerDefinition BeerDefinition => Definition as BeerDefinition;

        public override bool CanFire(bool keyDown)
        {
            return keyDown;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
            if (Object != null && Object.HasStateAuthority == false)
            {
                return;
            }

            var definition = BeerDefinition;
            if (definition == null)
            {
                return;
            }

            var character = Character;
            if (character == null)
            {
                return;
            }

            var agent = character.Agent;
            if (agent == null)
            {
                return;
            }

            var health = agent.Health;
            if (health == null || health.IsAlive == false)
            {
                return;
            }

            float healAmount = _overrideHealAmount > 0f ? _overrideHealAmount : definition.HealAmount;

            HitData hitData = new HitData
            {
                Action = EHitAction.Heal,
                Amount = healAmount,
                InstigatorRef = Object.InputAuthority,
                Target = health,
                HitType = EHitType.Heal,
            };

            ((IHitTarget)health).ProcessHit(ref hitData);
        }
    }
}
