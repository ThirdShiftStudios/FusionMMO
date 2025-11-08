#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

namespace Pinwheel.Vista.RealWorldData
{
    public abstract class DataProviderAsset : ScriptableObject
    {
        public DataRequest dataRequest { get; protected set; }

        [SerializeField]
        protected GeoRect m_longLat;
        public GeoRect longLat
        {
            get
            {
                return m_longLat;
            }
            set
            {
                m_longLat = value;                
            }
        }

        protected Texture2D m_heightMap;
        /// <summary>
        /// Return a direct reference to the height map constructed from height data. Used for immediate rendering, not readable from CPU side. This texture will be disposed on script reload and other events.
        /// </summary>
        public Texture2D heightMap
        {
            get
            {
                if (m_heightMap == null && Utilities.IsTextureDataValid(m_heightMapData, m_heightMapSize))
                {
                    m_heightMap = new Texture2D(m_heightMapSize.x, m_heightMapSize.y, TextureFormat.RFloat, false, true);
                    m_heightMap.SetPixelData(m_heightMapData, 0, 0);
                    m_heightMap.Apply(false, true);
                }
                return m_heightMap;
            }
        }

        [SerializeField]
        [HideInInspector]
        protected Vector2Int m_heightMapSize;
        [SerializeField]
        [HideInInspector]
        protected float[] m_heightMapData;

        public abstract float minHeight { get; }
        public abstract float maxHeight { get; }

        protected Texture2D m_colorMap;
        /// <summary>
        /// Return a direct reference to the color map constructed from color data. Used for immediate rendering, not readable from CPU side. This texture will be disposed on script reload and other events.
        /// </summary>
        public Texture2D colorMap
        {
            get
            {
                if (m_colorMap == null && Utilities.IsTextureDataValid(m_colorMapData, m_colorMapSize))
                {
                    m_colorMap = new Texture2D(m_colorMapSize.x, m_colorMapSize.y, TextureFormat.RGBA32, true, false);
                    m_colorMap.SetPixelData(m_colorMapData, 0, 0);
                    m_colorMap.Apply(true, true);
                }
                return m_colorMap;
            }
        }

        [SerializeField]
        [HideInInspector]
        protected Vector2Int m_colorMapSize;
        [SerializeField]
        [HideInInspector]
        protected Color32[] m_colorMapData;

        public abstract IRwdProvider provider { get; }

        public virtual void Reset()
        {
            m_longLat = GpsUtils.VIETNAM;
        }

        private void OnValidate()
        {
            if (m_longLat.maxX == m_longLat.minX)
                m_longLat.maxX += 0.01;
            if (m_longLat.maxY == m_longLat.minY)
                m_longLat.maxY += 0.01;
        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {
            if (m_heightMap != null)
            {
                UnityEngine.Object.DestroyImmediate(m_heightMap);
            }

            if (m_colorMap != null)
            {
                UnityEngine.Object.DestroyImmediate(m_colorMap);
            }
        }

        public virtual ProgressiveTask RequestAndSaveAll()
        {
            ProgressiveTask task = new ProgressiveTask();
            CoroutineUtility.StartCoroutine(IRequestAndSaveAll(task));
            return task;
        }

        private IEnumerator IRequestAndSaveAll(ProgressiveTask task)
        {
            if (provider.availability.HasFlag(DataAvailability.HeightMap))
                yield return RequestAndSaveHeightMap();
            if (provider.availability.HasFlag(DataAvailability.ColorMap))
                yield return RequestAndSaveColorMap();
            task.Complete();
        }

        protected void SetHeightMap(float[] data, Vector2Int size)
        {
            if (!Utilities.IsTextureDataValid(data, size))
            {
                throw new System.ArgumentException("Height map data not valid, check data size");
            }

            m_heightMapSize = size;
            m_heightMapData = data;

            if (m_heightMap != null)
            {
                UnityEngine.Object.DestroyImmediate(m_heightMap);
            }
        }

        protected void SetColorMap(Color32[] data, Vector2Int size)
        {
            if (!Utilities.IsTextureDataValid(data, size))
            {
                throw new System.ArgumentException("Color map data not valid, check data size");
            }

            m_colorMapSize = size;
            m_colorMapData = data;

            if (m_colorMap != null)
            {
                UnityEngine.Object.DestroyImmediate(m_colorMap);
            }
        }

        public ProgressiveTask RequestAndSaveHeightMap()
        {
            ProgressiveTask task = new ProgressiveTask();
            CoroutineUtility.StartCoroutine(IRequestAndSaveHeightMap(task));
            return task;
        }

        private IEnumerator IRequestAndSaveHeightMap(ProgressiveTask task)
        {
            dataRequest = RequestHeightMap(m_longLat);
            yield return dataRequest;
            if (Utilities.IsTextureDataValid(dataRequest.heightMapData, dataRequest.heightMapSize))
            {
                SetHeightMap(dataRequest.heightMapData, dataRequest.heightMapSize);
            }
            task.Complete();
        }

        public virtual DataRequest RequestHeightMap(GeoRect gps) => DataRequest.DoneAndEmpty();

        public ProgressiveTask RequestAndSaveColorMap()
        {
            ProgressiveTask task = new ProgressiveTask();
            CoroutineUtility.StartCoroutine(IRequestAndSaveColorMap(task));
            return task;
        }

        private IEnumerator IRequestAndSaveColorMap(ProgressiveTask task)
        {
            dataRequest = RequestColorMap(m_longLat);
            yield return dataRequest;
            if (Utilities.IsTextureDataValid(dataRequest.colorMapData, dataRequest.colorMapSize))
            {
                SetColorMap(dataRequest.colorMapData, dataRequest.colorMapSize);
            }
            task.Complete();
        }

        public virtual DataRequest RequestColorMap(GeoRect gps) => DataRequest.DoneAndEmpty();
    }
}
#endif
