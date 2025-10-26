using System;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "FishDefinition", menuName = "TSS/Data Definitions/Fish")]
    public sealed class FishDefinition : DataDefinition
    {
        [SerializeField]
        private string _displayName;
        [SerializeField]
        private Texture2D _icon;
        [SerializeField]
        private FishItem _fishPrefab;

        public FishItem FishPrefab => _fishPrefab;

        public override string Name => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public override Texture2D Icon => _icon;
    }
}
