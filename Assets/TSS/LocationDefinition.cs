using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "LocationDefinition", menuName = "TSS/Data Definitions/Location")]
    public class LocationDefinition : DataDefinition
    {
        [SerializeField]
        private string _displayName;

        [SerializeField]
        private Texture2D _icon;

        public override string Name => _displayName;

        public override Texture2D Icon => _icon;
    }
}
