using System;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    [Serializable]
    public struct LootTableItem
    {
        public ItemDefinition ItemDefinition;

        [Range(0f, 100f)]
        public float Probability;

        public int MinimumLuck;
    }
}
