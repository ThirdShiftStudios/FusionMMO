using TSS.Data;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [CreateAssetMenu(fileName = "BeerDefinition", menuName = "TSS/Data Definitions/Consumables/Beer")]
    public class BeerDefinition : WeaponDefinition
    {
        [SerializeField]
        [Tooltip("Amount of health restored when the beer is consumed.")]
        private float _healAmount = 25f;

        public float HealAmount => _healAmount;

        public override ESlotCategory SlotCategory => ESlotCategory.Consumable;
    }
}
