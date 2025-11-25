using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "BurnBuff", menuName = "TSS/Data Definitions/Buffs/Burn")]
    public sealed class BurnBuff : BuffDefinition
    {
        [SerializeField, Min(0f)]
        private float _damagePerTick = 5f;
        [SerializeField, Min(0.1f)]
        private float _tickInterval = 2f;

        private readonly Dictionary<BuffSystem, float> _tickTimers = new Dictionary<BuffSystem, float>();

        public float TickInterval => Mathf.Max(0.01f, _tickInterval);

        public override void OnAdd(BuffSystem buffSystem, ref BuffData data, int previousStacks)
        {
            if (buffSystem == null)
            {
                return;
            }

            if (_tickTimers.ContainsKey(buffSystem) == false)
            {
                _tickTimers[buffSystem] = TickInterval;
            }
        }

        public override void OnTick(BuffSystem buffSystem, ref BuffData data, float deltaTime)
        {
            if (buffSystem == null || data.Stacks == 0)
            {
                return;
            }

            if (_tickTimers.TryGetValue(buffSystem, out float timer) == false)
            {
                timer = 0f;
            }

            timer -= deltaTime;

            if (timer <= 0f)
            {
                ApplyDamage(buffSystem, data.Source);
                buffSystem.RegisterTick(this);
                timer += TickInterval;
            }

            _tickTimers[buffSystem] = timer;
        }

        public override void OnRemove(BuffSystem buffSystem, ref BuffData data)
        {
            if (buffSystem == null)
            {
                return;
            }

            _tickTimers.Remove(buffSystem);
        }

        private void ApplyDamage(BuffSystem buffSystem, PlayerRef source)
        {
            if (_damagePerTick <= 0f || buffSystem.HasStateAuthority == false)
            {
                return;
            }

            Agent agent = buffSystem.Agent;
            Health health = agent != null ? agent.Health : buffSystem.GetComponent<Health>();

            if (health == null)
            {
                return;
            }

            Vector3 position = health.transform.position;

            HitData hitData = new HitData
            {
                Action = EHitAction.Damage,
                Amount = _damagePerTick,
                Position = position,
                Normal = Vector3.up,
                Direction = Vector3.zero,
                InstigatorRef = source != PlayerRef.None
                    ? source
                    : (buffSystem.Object != null ? buffSystem.Object.InputAuthority : PlayerRef.None),
                Target = health,
                HitType = EHitType.Suicide,
            };

            HitUtility.ProcessHit(ref hitData);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var serializedObject = new UnityEditor.SerializedObject(this);
            serializedObject.FindProperty("_duration").floatValue = 10f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
#endif
    }
}
