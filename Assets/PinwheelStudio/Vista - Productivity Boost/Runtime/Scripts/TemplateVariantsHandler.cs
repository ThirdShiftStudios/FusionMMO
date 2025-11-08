#if VISTA
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pinwheel.Vista.ProductivityBoost
{
    public static class TemplateVariantsHandler
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void OnInitialize()
        {
            TemplateUtils.enableVariantsSupportCallback += EnableVariantsSupport;
        }

        private static bool EnableVariantsSupport()
        {
            return true;
        }
    }
}
#endif
