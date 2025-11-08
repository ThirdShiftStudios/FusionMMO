#if VISTA
#if __MICROSPLAT__
using UnityEditor;
using Pinwheel.VistaEditor.Graph;
using Pinwheel.Vista.Graph;

namespace Pinwheel.VistaEditor.MicroSplatIntegration
{
    [InitializeOnLoad]
    public static class GraphConstantsInitializer
    {
        [InitializeOnLoadMethod]
        public static void InitOutputNameSelector()
        {
#if __MICROSPLAT_STREAMS__
            OutputNodeEditor.nameSelector.Add(new NameSelectorEntry()
            {
                name = GraphConstants.STREAM_DATA_OUTPUT_NAME,
                slotType = typeof(ColorTextureSlot)
            });
#endif
#if __MICROSPLAT_SNOW__
            OutputNodeEditor.nameSelector.Add(new NameSelectorEntry()
            {
                name = GraphConstants.SNOW_MASK_OUTPUT_NAME,
                slotType = typeof(ColorTextureSlot)
            });
#endif
#if __MICROSPLAT_SCATTER__
            OutputNodeEditor.nameSelector.Add(new NameSelectorEntry()
            {
                name = GraphConstants.SCATTER_OUTPUT_NAME,
                slotType = typeof(ColorTextureSlot)
            });
#endif
#if __MICROSPLAT_PROCTEX__
            OutputNodeEditor.nameSelector.Add(new NameSelectorEntry()
            {
                name = GraphConstants.CAVITY_OUTPUT_NAME,
                slotType = typeof(ColorTextureSlot)
            });
#endif
#if __MICROSPLAT_GLOBALTEXTURE__
            OutputNodeEditor.nameSelector.Add(new NameSelectorEntry()
            {
                name = GraphConstants.GLOBAL_TINT_OUTPUT_NAME,
                slotType = typeof(ColorTextureSlot)
            }); 
            OutputNodeEditor.nameSelector.Add(new NameSelectorEntry()
            {
                name = GraphConstants.GLOBAL_SAOM_OUTPUT_NAME,
                slotType = typeof(ColorTextureSlot)
            }); 
            OutputNodeEditor.nameSelector.Add(new NameSelectorEntry()
            {
                name = GraphConstants.GLOBAL_EMIS_OUTPUT_NAME,
                slotType = typeof(ColorTextureSlot)
            });
#endif
        }
    }
}
#endif
#endif