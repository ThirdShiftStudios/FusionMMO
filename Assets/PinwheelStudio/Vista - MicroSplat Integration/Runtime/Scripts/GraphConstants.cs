#if VISTA
#if __MICROSPLAT__

namespace Pinwheel.VistaEditor.MicroSplatIntegration
{
    public static class GraphConstants
    {
#if __MICROSPLAT_STREAMS__
        public const string STREAM_DATA_OUTPUT_NAME = "MicroSplat Stream Data";
#endif
#if __MICROSPLAT_SNOW__
        public const string SNOW_MASK_OUTPUT_NAME = "MicroSplat Snow Mask";
#endif
#if __MICROSPLAT_SCATTER__
        public const string SCATTER_OUTPUT_NAME = "MicroSplat Scatter";
#endif
#if __MICROSPLAT_PROCTEX__
        public const string CAVITY_OUTPUT_NAME = "MicroSplat Cavity";
#endif
#if __MICROSPLAT_GLOBALTEXTURE__
        public const string GLOBAL_TINT_OUTPUT_NAME = "MicroSplat Global Tint";
        public const string GLOBAL_SAOM_OUTPUT_NAME = "MicroSplat Global SAOM";
        public const string GLOBAL_EMIS_OUTPUT_NAME = "MicroSplat Global Emissive";
#endif
    }
}
#endif
#endif