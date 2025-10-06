using UnityEngine;
using UnityEngine.Serialization;

namespace TSS.Data
{
    public abstract class InventoryItem : DataDefinition
    {
        [FormerlySerializedAs("WorldVisuals")]
        [SerializeField]
        private GameObject _worldGraphic;

        public GameObject WorldGraphic => _worldGraphic;
    }
}
