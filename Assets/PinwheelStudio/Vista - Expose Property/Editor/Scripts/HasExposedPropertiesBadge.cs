#if VISTA
using Pinwheel.VistaEditor.UIElements;
using UnityEngine;

namespace Pinwheel.VistaEditor.ExposeProperty
{
    public class HasExposedPropertiesBadge : Badge
    {
        public HasExposedPropertiesBadge() : base()
        {
            icon = Resources.Load<Texture>("Vista/Textures/HasExposedProperties");
            tooltip = "This node has some of its properties exposed.";
        }
    }
}
#endif
