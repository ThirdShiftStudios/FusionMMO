#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEngine.Networking;

namespace Pinwheel.Vista.RealWorldData
{
    public class DataRequest : ProgressiveTask
    {
        private ProgressHandler m_progressHandler;
        public float progress => m_progressHandler != null ? m_progressHandler.value : 0;

        public Vector2Int heightMapSize { get; set; }
        public float[] heightMapData { get; set; }

        public Vector2Int colorMapSize { get; set; }
        public Color32[] colorMapData { get; set; }

        public long id { get; private set; }

        public DataRequest(ProgressHandler progressHandler = null)
        {
            id = System.DateTime.Now.Ticks;
            m_progressHandler = progressHandler;
        }

        public static DataRequest DoneAndEmpty()
        {
            DataRequest r = new DataRequest();
            r.Complete();
            return r;
        }
    }
}
#endif
