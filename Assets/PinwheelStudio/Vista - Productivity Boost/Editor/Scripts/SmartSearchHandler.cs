#if VISTA
using Pinwheel.VistaEditor.Graph;
using UnityEditor;

namespace Pinwheel.VistaEditor.ProductivityBoost
{
    public static class SmartSearchHandler
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            SearcherUtils.enableSmartSearchCallback += EnableSmartSearch;
        }

        private static bool EnableSmartSearch()
        {
            return true;
        }
    }
}
#endif
