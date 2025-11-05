using System.Collections.Generic;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    public interface IExperienceMultiplierBuff
    {
        float GetExperienceMultiplier(BuffSystem buffSystem, BuffData data);
    }

    public interface IMovementSpeedMultiplierBuff
    {
        float GetMovementSpeedMultiplier(BuffSystem buffSystem, BuffData data);
    }

    public abstract class BuffDefinition : DataDefinition
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Texture2D _icon;
        [SerializeField] private bool _isStackable = true;
        [SerializeField, Min(1)] private int _maxStacks = 1;
        [SerializeField, Min(0f)] private float _duration = 0f;

        private static Dictionary<int, BuffDefinition> _definitions;

        public override string Name => _displayName;
        public override Texture2D Icon => _icon;

        public bool IsStackable => _isStackable;
        public int MaxStacks => Mathf.Max(1, _maxStacks);
        public float Duration => Mathf.Max(0f, _duration);

        public abstract void OnAdd(BuffSystem buffSystem, ref BuffData data, int previousStacks);
        public abstract void OnTick(BuffSystem buffSystem, ref BuffData data, float deltaTime);
        public abstract void OnRemove(BuffSystem buffSystem, ref BuffData data);

        public static BuffDefinition Get(int id)
        {
            if (id == 0)
            {
                return null;
            }

            if (_definitions == null)
            {
                LoadAll();
            }

            _definitions.TryGetValue(id, out BuffDefinition definition);
            return definition;
        }

        public static void LoadAll()
        {
            _definitions = new Dictionary<int, BuffDefinition>();

            BuffDefinition[] definitions = Resources.LoadAll<BuffDefinition>(string.Empty);
            for (int i = 0; i < definitions.Length; ++i)
            {
                BuffDefinition definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                _definitions[definition.ID] = definition;
            }
        }
    }
}
