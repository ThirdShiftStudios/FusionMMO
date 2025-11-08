#if VISTA
#if __MICROSPLAT__
using UnityEditor;
using Pinwheel.VistaEditor.Graph;

namespace Pinwheel.VistaEditor.MicroSplatIntegration
{
    public static class RgbaMergeLabelsInitializer
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
#if __MICROSPLAT_STREAMS__
            RgbaMergeNodeEditor.labels.Add(new RgbaMergeNodeEditor.Labels()
            {
                title = "MS Stream",
                rLabel = "Wetness",
                gLabel = "Puddles",
                bLabel = "Streams",
                aLabel = "Lava"
            });
#endif
#if __MICROSPLAT_SNOW__
            RgbaMergeNodeEditor.labels.Add(new RgbaMergeNodeEditor.Labels()
            {
                title = "MS Snow Mask",
                rLabel = "Snow Max",
                gLabel = "Snow Min",
                bLabel = "(unused)",
                aLabel = "(unused)"
            });
#endif
#if __MICROSPLAT_PROCTEX__
            RgbaMergeNodeEditor.labels.Add(new RgbaMergeNodeEditor.Labels()
            {
                title = "MS Cavity",
                rLabel = "(unused)",
                gLabel = "Erosion",
                bLabel = "(unused)",
                aLabel = "Cavity"
            });
#endif
#if __MICROSPLAT_GLOBALTEXTURE__
            RgbaMergeNodeEditor.labels.Add(new RgbaMergeNodeEditor.Labels()
            {
                title = "MS Global SAOM",
                rLabel = "Smoothness",
                gLabel = "AO",
                bLabel = "Metallic",
                aLabel = "(unused)"
            });
#endif
        }
    }
}
#endif
#endif