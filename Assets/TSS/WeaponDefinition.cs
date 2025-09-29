using TSS.Data;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [System.Serializable]
    public struct WeaponAttachmentOffsets
    {
        public Vector3 Position;
        public Vector3 RotationEuler;

        public Quaternion Rotation => Quaternion.Euler(RotationEuler);
    }

    public class WeaponDefinition : ItemDefinition
    {
        [Header("Prefabs")]
        public GameObject WeaponGhostPrefab;

        [Header("Animation")]
        public RuntimeAnimatorController ThirdPersonAnimatorController;
        public AnimationClip EquipClip;
        public AnimationClip UnequipClip;
        public AnimationClip PrimaryAttackClip;

        [Header("Attachment Offsets")]
        public WeaponAttachmentOffsets ThirdPersonAttachment;

        [Header("Melee Settings")]
        public float Damage = 25f;
        public float Range = 2f;
        public float AttackAngleDegrees = 120f;
        public float CooldownSeconds = 0.75f;
    }
}
