using System;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "FishDefinition", menuName = "TSS/Data Definitions/Fish")]
    public sealed class FishDefinition : ItemDefinition
    {
        [SerializeField]
        private FishItem _fishPrefab;

        public FishItem FishPrefab => _fishPrefab;
    }
}
