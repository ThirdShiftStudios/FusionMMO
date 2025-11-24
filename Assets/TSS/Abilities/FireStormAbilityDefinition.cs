using Fusion;
using TPSBR;
using TPSBR.Abilities;
using UnityEngine;

namespace TPSBR.Abilities
{
    [CreateAssetMenu(fileName = "FireStormAbilityDefinition", menuName = "TSS/Abilities/Fire Storm Ability")]
    public class FireStormAbilityDefinition : StaffAbilityDefinition, ISelectBeforeCast, IAbilityImpact
    {
        public const string AbilityCode = "FIRESTORM";

        private const string LogPrefix = "[<color=#FFA500>FireStormAbility</color>]";

        [Header("Cast Settings")]
        [SerializeField]
        private GameObject _castIndicatorGraphic;
        [SerializeField]
        private float _maxTargetDistance = 30f;
        [SerializeField]
        private float _tickInterval = 0.75f;

        [Header("Prefab")]
        [SerializeField]
        private NetworkPrefabRef _fireStormPrefab;

        [Header("Impact")]
        [SerializeField]
        private GameObject _impactGraphic;

        public GameObject CastIndicatorGraphic => _castIndicatorGraphic;
        public FireStormAbilityUpgradeData FireStormUpgradeData => GetUpgradeData<FireStormAbilityUpgradeData>();

        public GameObject ImpactGraphic => _impactGraphic;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetStringCode(AbilityCode);
            EnsureUpgradeData<FireStormAbilityUpgradeData>();
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

            if (_fireStormPrefab.IsValid == false)
            {
                Debug.LogWarning($"{LogPrefix} Fire Storm prefab is not assigned.");
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

            Transform cameraTransform = character.ThirdPersonView.DefaultCameraTransform;

            if (cameraTransform == null)
            {
                return;
            }

            Vector3 targetPosition;
            if (staffWeapon.TryConsumeSelectedCastPosition(out Vector3 selectedPosition) == true)
            {
                targetPosition = selectedPosition;
            }
            else
            {
                Vector3 origin = cameraTransform.position;
                Vector3 direction = cameraTransform.forward;

                if (runner.GetPhysicsScene().Raycast(origin, direction, out RaycastHit hitInfo, _maxTargetDistance, ~0, QueryTriggerInteraction.Ignore) == true)
                {
                    targetPosition = hitInfo.point;
                }
                else
                {
                    targetPosition = origin + direction * _maxTargetDistance;
                }
            }

            LayerMask hitMask = character.Agent != null && character.Agent.Inventory != null ? character.Agent.Inventory.HitMask : default;
            NetworkObject owner = staffWeapon.Owner;

            if (owner == null)
            {
                return;
            }

            FireStormAbilityLevelData levelData = ResolveUpgradeLevel(staffWeapon);

            runner.Spawn(_fireStormPrefab, targetPosition, Quaternion.identity, owner.InputAuthority, (spawnRunner, spawnedObject) =>
            {
                FireStormAbility fireStorm = spawnedObject.GetComponent<FireStormAbility>();

                if (fireStorm == null)
                {
                    return;
                }

                fireStorm.Configure(owner, hitMask, staffWeapon.HitType, levelData.Damage, levelData.Duration, levelData.Radius, _tickInterval);
            });
        }

        private FireStormAbilityLevelData ResolveUpgradeLevel(StaffWeapon staffWeapon)
        {
            FireStormAbilityUpgradeData upgradeData = FireStormUpgradeData;

            if (upgradeData == null)
            {
                return new FireStormAbilityLevelData
                {
                    CastingTime = BaseCastTime
                };
            }

            int abilityLevel = staffWeapon != null ? staffWeapon.GetAbilityLevel(this) : 1;
            return upgradeData.GetLevelData(abilityLevel);
        }
    }
}
