using Fusion;
using TPSBR;
using UnityEngine;

namespace TPSBR.Abilities
{
    [CreateAssetMenu(fileName = "FireballAbilityDefinition", menuName = "TSS/Abilities/Fireball Ability")]
    public class FireballAbilityDefinition : StaffAbilityDefinition
    {
        public const string AbilityCode = "FIREBALL";

        private const string LogPrefix = "[<color=#FFA500>FireballAbility</color>]";

        [Header("Projectile")]
        [SerializeField]
        private NetworkPrefabRef _projectilePrefab;
        [SerializeField]
        private float _projectileSpeed = 30f;
        [SerializeField]
        private float _targetDistance = 40f;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetStringCode(AbilityCode);
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

            if (_projectilePrefab.IsValid == false)
            {
                Debug.LogWarning($"{LogPrefix} Fireball projectile prefab is not assigned.");
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
            Transform cameraTransform = character.ThirdPersonView.DefaultCameraTransform;

            if (fireTransform == null || cameraTransform == null)
            {
                return;
            }

            Vector3 firePosition = fireTransform.position;
            Vector3 targetPoint = cameraTransform.position + cameraTransform.forward * _targetDistance;
            Vector3 direction = targetPoint - firePosition;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = fireTransform.forward;
            }

            direction.Normalize();

            Vector3 initialVelocity = direction * _projectileSpeed;
            LayerMask hitMask = character.Agent != null && character.Agent.Inventory != null ? character.Agent.Inventory.HitMask : default;
            NetworkObject owner = staffWeapon.Owner;

            if (owner == null)
            {
                return;
            }

            runner.Spawn(_projectilePrefab, firePosition, Quaternion.LookRotation(direction), owner.InputAuthority, (spawnRunner, spawnedObject) =>
            {
                FireballProjectile projectile = spawnedObject.GetComponent<FireballProjectile>();

                if (projectile == null)
                {
                    return;
                }

                projectile.Fire(owner, firePosition, initialVelocity, hitMask, staffWeapon.HitType);
            });
        }
    }
}
