using Fusion;
using UnityEngine;

namespace TPSBR.Abilities
{
    [CreateAssetMenu(fileName = "FireNovaAbilityDefinition", menuName = "TSS/Abilities/Fire Nova Ability")]
    public class FireNovaDefinition : StaffAbilityDefinition
    {
        public const string AbilityCode = "FIRENOVA";

        private const string LogPrefix = "[<color=#FFA500>FireNovaAbility</color>]";

        [Header("Projectile")]
        [SerializeField]
        private NetworkPrefabRef _fireNovaPrefab;
        [SerializeField]
        private float _projectileSpeed = 30f;
        [SerializeField]
        private float _targetDistance = 40f;

        public FireNovaAbilityUpgradeData FireNovaUpgradeData => GetUpgradeData<FireNovaAbilityUpgradeData>();

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetStringCode(AbilityCode);
            EnsureUpgradeData<FireNovaAbilityUpgradeData>();
        }
#endif

        public override void Execute(StaffWeapon staffWeapon)
        {
            if (staffWeapon == null)
            {
                Debug.LogWarning($"{LogPrefix} Attempted to execute without a valid staff weapon.");
                return;
            }

            if (staffWeapon.HasStateAuthority == false)
            {
                return;
            }

            if (_fireNovaPrefab.IsValid == false)
            {
                Debug.LogWarning($"{LogPrefix} Fire Nova projectile prefab is not assigned.");
                return;
            }

            var runner = staffWeapon.Runner;

            if (runner == null || runner.IsRunning == false)
            {
                return;
            }

            var character = staffWeapon.Character;

            if (character == null || character.ThirdPersonView == null)
            {
                return;
            }

            Transform fireTransform = character.ThirdPersonView.FireTransform;

            if (fireTransform == null)
            {
                return;
            }

            Vector3 firePosition = character.transform.position;

            LayerMask hitMask = character.Agent != null && character.Agent.Inventory != null ? character.Agent.Inventory.HitMask : default;
            NetworkObject owner = staffWeapon.Owner;

            if (owner == null)
            {
                return;
            }

            FireNovaAbilityUpgradeLevel levelData = ResolveUpgradeLevel(staffWeapon);

            runner.Spawn(_fireNovaPrefab, firePosition, default, owner.InputAuthority, (spawnRunner, spawnedObject) =>
            {
                FireNova projectile = spawnedObject.GetComponent<FireNova>();

                if (projectile == null)
                {
                    return;
                }

                projectile.ConfigureLevel(levelData.Radius, levelData.Damage, levelData.BurnDuration, levelData.BurnDamage);
                projectile.StartNova(owner, firePosition, hitMask, staffWeapon.HitType);
            });
        }

        private FireNovaAbilityUpgradeLevel ResolveUpgradeLevel(StaffWeapon staffWeapon)
        {
            FireNovaAbilityUpgradeData upgradeData = FireNovaUpgradeData;

            if (upgradeData == null || upgradeData.LevelCount == 0)
            {
                return new FireNovaAbilityUpgradeLevel
                {
                    CastingTime = BaseCastTime
                };
            }

            int abilityLevel = staffWeapon != null ? staffWeapon.GetAbilityLevel(this) : 1;
            return upgradeData.GetLevelData(abilityLevel);
        }
    }
}
