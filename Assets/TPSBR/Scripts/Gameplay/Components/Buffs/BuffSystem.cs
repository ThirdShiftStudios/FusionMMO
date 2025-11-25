using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public struct BuffData : INetworkStruct
    {
        public int   DefinitionId;
        public byte  Stacks;
        public float RemainingTime;
        public PlayerRef Source;

        public bool IsValid => DefinitionId != 0 && Stacks > 0;

        public void Clear()
        {
            DefinitionId = 0;
            Stacks = 0;
            RemainingTime = 0f;
            Source = PlayerRef.None;
        }
    }

    public sealed class BuffSystem : ContextBehaviour
    {
        public const int MaxBuffSlots = 16;

        [Networked, Capacity(MaxBuffSlots)]
        private NetworkArray<BuffData> _activeBuffs { get; }

        private Agent       _agent;
        private AgentSenses _senses;
        private readonly GameObject[] _activeTickGraphics = new GameObject[MaxBuffSlots];

        public Agent Agent => _agent;
        public AgentSenses Senses => _senses;

        private void Awake()
        {
            _agent = GetComponent<Agent>();
            _senses = GetComponent<AgentSenses>();
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            float deltaTime = Runner.DeltaTime;

            for (int i = 0; i < MaxBuffSlots; ++i)
            {
                BuffData data = _activeBuffs.Get(i);
                if (data.IsValid == false)
                {
                    continue;
                }

                BuffDefinition definition = BuffDefinition.Get(data.DefinitionId);
                if (definition == null)
                {
                    data.Clear();
                    _activeBuffs.Set(i, data);
                    continue;
                }

                definition.OnTick(this, ref data, deltaTime);

                bool shouldRemove = data.Stacks == 0 || data.DefinitionId == 0;

                if (shouldRemove == false && definition.Duration > 0f)
                {
                    data.RemainingTime = Mathf.Max(0f, data.RemainingTime - deltaTime);
                    if (data.RemainingTime <= 0f)
                    {
                        shouldRemove = true;
                    }
                }

                if (shouldRemove == true)
                {
                    RemoveBuffInternal(i, definition, ref data);
                    continue;
                }

                _activeBuffs.Set(i, data);
            }
        }

        public void ApplyBuff(BuffDefinition definition, int stacks = 1, PlayerRef source = default)
        {
            if (definition == null || stacks <= 0)
            {
                return;
            }

            if (HasStateAuthority == false)
            {
                return;
            }

            ApplyBuffInternal(definition, stacks, source);
        }

        private void ApplyBuffInternal(BuffDefinition definition, int stacks, PlayerRef source)
        {
            int index = FindBuffIndex(definition.ID);
            if (index < 0)
            {
                index = FindEmptySlot();
                if (index < 0)
                {
                    return;
                }

                var newData = new BuffData
                {
                    DefinitionId = definition.ID,
                    Stacks = 0,
                    RemainingTime = 0f,
                    Source = PlayerRef.None,
                };

                _activeBuffs.Set(index, newData);
            }

            BuffData data = _activeBuffs.Get(index);
            if (data.DefinitionId != definition.ID)
            {
                ClearTickGraphic(index);
                data.DefinitionId = definition.ID;
                data.Stacks = 0;
                data.RemainingTime = 0f;
                data.Source = PlayerRef.None;
            }

            stacks = Mathf.Max(1, stacks);

            int maxStacks = Mathf.Clamp(definition.MaxStacks, 1, byte.MaxValue);

            for (int i = 0; i < stacks; ++i)
            {
                int previousStacks = data.Stacks;

                if (definition.IsStackable == true)
                {
                    if (data.Stacks < maxStacks)
                    {
                        data.Stacks = (byte)Mathf.Min(data.Stacks + 1, maxStacks);
                    }
                }
                else
                {
                    data.Stacks = (byte)1;
                }

                if (definition.Duration > 0f)
                {
                    data.RemainingTime += definition.Duration;
                }

                definition.OnAdd(this, ref data, previousStacks);
            }

            if (data.Stacks == 0)
            {
                data.Stacks = (byte)1;
            }

            if (source != PlayerRef.None)
            {
                data.Source = source;
            }

            _activeBuffs.Set(index, data);

            SpawnTickGraphic(definition, index);
        }

        public void RemoveBuff(BuffDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            RemoveBuff(definition.ID);
        }

        public void RemoveBuff(int definitionId)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            int index = FindBuffIndex(definitionId);
            if (index < 0)
            {
                return;
            }

            BuffData data = _activeBuffs.Get(index);
            BuffDefinition definition = BuffDefinition.Get(data.DefinitionId);
            RemoveBuffInternal(index, definition, ref data);
        }


        public float GetExperienceMultiplier()
        {
            float total = 1f;

            for (int i = 0; i < MaxBuffSlots; ++i)
            {
                BuffData data = _activeBuffs.Get(i);
                if (data.IsValid == false)
                {
                    continue;
                }

                BuffDefinition definition = BuffDefinition.Get(data.DefinitionId);
                if (definition is IExperienceMultiplierBuff experienceBuff)
                {
                    total *= Mathf.Max(0f, experienceBuff.GetExperienceMultiplier(this, data));
                }
            }

            return Mathf.Max(0f, total);
        }

        public float GetMovementSpeedMultiplier()
        {
            float total = 1f;

            for (int i = 0; i < MaxBuffSlots; ++i)
            {
                BuffData data = _activeBuffs.Get(i);
                if (data.IsValid == false)
                {
                    continue;
                }

                BuffDefinition definition = BuffDefinition.Get(data.DefinitionId);
                if (definition is IMovementSpeedMultiplierBuff speedBuff)
                {
                    total *= Mathf.Max(0f, speedBuff.GetMovementSpeedMultiplier(this, data));
                }
            }

            return Mathf.Max(0f, total);
        }

        public bool TryGetBuff(BuffDefinition definition, out BuffData data)
        {
            data = default;
            if (definition == null)
            {
                return false;
            }

            int index = FindBuffIndex(definition.ID);
            if (index < 0)
            {
                return false;
            }

            data = _activeBuffs.Get(index);
            return data.IsValid;
        }

        public int GetActiveBuffs(List<BuffData> buffer)
        {
            if (buffer == null)
            {
                return 0;
            }

            buffer.Clear();

            for (int i = 0; i < MaxBuffSlots; ++i)
            {
                BuffData data = _activeBuffs.Get(i);
                if (data.IsValid == false)
                {
                    continue;
                }

                buffer.Add(data);
            }

            return buffer.Count;
        }

        private int FindBuffIndex(int definitionId)
        {
            if (definitionId == 0)
            {
                return -1;
            }

            for (int i = 0; i < MaxBuffSlots; ++i)
            {
                BuffData data = _activeBuffs.Get(i);
                if (data.IsValid == true && data.DefinitionId == definitionId)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < MaxBuffSlots; ++i)
            {
                BuffData data = _activeBuffs.Get(i);
                if (data.IsValid == false)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RemoveBuffInternal(int index, BuffDefinition definition, ref BuffData data)
        {
            definition?.OnRemove(this, ref data);
            ClearTickGraphic(index);
            data.Clear();
            _activeBuffs.Set(index, data);
        }

        private void SpawnTickGraphic(BuffDefinition definition, int index)
        {
            if (definition == null || definition.OnTickGraphic == null)
            {
                return;
            }

            if (index < 0 || index >= MaxBuffSlots || _activeTickGraphics[index] != null)
            {
                return;
            }

            IHitTarget hitTarget = GetComponent<IHitTarget>();
            Transform parent = hitTarget?.AbilityHitPivot;
            if (parent == null)
            {
                return;
            }

            _activeTickGraphics[index] = Instantiate(definition.OnTickGraphic, parent);
        }

        private void ClearTickGraphic(int index)
        {
            if (index < 0 || index >= MaxBuffSlots)
            {
                return;
            }

            GameObject graphic = _activeTickGraphics[index];
            if (graphic != null)
            {
                Destroy(graphic);
                _activeTickGraphics[index] = null;
            }
        }
    }
}
