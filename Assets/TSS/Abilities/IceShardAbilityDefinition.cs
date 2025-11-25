using Fusion;
using TPSBR;
using TPSBR.Abilities;
using UnityEngine;

[CreateAssetMenu(fileName = "IceShardAbilityDefinition", menuName = "TSS/Abilities/Ice Shard Ability")]
public class IceShardAbilityDefinition: StaffAbilityDefinition, IAbilityImpact
{
    public const string AbilityCode = "ICESHARD";

    private const string LogPrefix = "[<color=#FFA500>IceShardAbility</color>]";

    [Header("Projectile")]
    [SerializeField]
    private NetworkPrefabRef _projectilePrefab;
    [SerializeField]
    private float _projectileSpeed = 30f;
    [SerializeField]
    private float _targetDistance = 40f;

    [Header("Impact")]
    [SerializeField]
    private GameObject _impactGraphic;

    public IceShardAbilityUpgradeData IceShardUpgradeData => GetUpgradeData<IceShardAbilityUpgradeData>();

    public GameObject ImpactGraphic => _impactGraphic;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetStringCode(AbilityCode);
        EnsureUpgradeData<IceShardAbilityUpgradeData>();
    }
#endif

    private void OnEnable()
    {
        AbilityImpactRegistry.Register(_impactGraphic);
    }

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

        IceShardAbilityLevelData levelData = ResolveUpgradeLevel(staffWeapon);

        runner.Spawn(_projectilePrefab, firePosition, Quaternion.LookRotation(direction), owner.InputAuthority, (spawnRunner, spawnedObject) =>
        {
            IceShardProjectile projectile = spawnedObject.GetComponent<IceShardProjectile>();

            if (projectile == null)
            {
                return;
            }

            projectile.ConfigureImpactGraphic(_impactGraphic);
            projectile.ConfigureBuff(BuffDefinition);
            projectile.ConfigureDamage(levelData.Damage);
            projectile.Fire(owner, firePosition, initialVelocity, hitMask, staffWeapon.HitType);
        });
    }

    private IceShardAbilityLevelData ResolveUpgradeLevel(StaffWeapon staffWeapon)
    {
        IceShardAbilityUpgradeData upgradeData = IceShardUpgradeData;

        if (upgradeData == null)
        {
            return new IceShardAbilityLevelData
            {
                CastingTime = BaseCastTime
            };
        }

        int abilityLevel = staffWeapon != null ? staffWeapon.GetAbilityLevel(this) : 1;
        return upgradeData.GetLevelData(abilityLevel);
    }
}
