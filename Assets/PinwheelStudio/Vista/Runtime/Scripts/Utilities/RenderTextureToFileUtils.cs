#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.Graph;

namespace Pinwheel.Vista
{
    public static class RenderTextureToFileUtils
    {
        public delegate void RTToFileHandler(RenderTexture rt, string fileNameNoExtension);
        public static event RTToFileHandler saveRenderTextureCallback;

        public static void SignalSaveRenderTextureToFile(RenderTexture rt, string fileNameNoExtension)
        {
            if (rt == null)
            {
                throw new System.ArgumentNullException("rt cannot be null");
            }

            saveRenderTextureCallback?.Invoke(rt, fileNameNoExtension);
        }

        internal static string GetFileNameNoExtension(INode node, ISlot slot)
        {
            string path = $"{NodeMetadata.Get(node.GetType()).title}_{node.id.Substring(0,8)}_{slot.name}";
            return path;
        }
    }
}
#endif
