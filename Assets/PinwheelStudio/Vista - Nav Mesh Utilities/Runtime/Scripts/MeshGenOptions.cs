#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Vista.NavMeshUtilities
{
    /// <summary>
    /// Contains all options for the conversion pipeline, including world size, mesh density, etc.
    /// </summary>
    [System.Serializable]
    public struct MeshGenOptions
    {
        [SerializeField]
        internal float m_width;
        public float width
        {
            get
            {
                return m_width;
            }
            set
            {
                m_width = Mathf.Max(1, value);
            }
        }

        [SerializeField]
        internal float m_height;
        public float height
        {
            get
            {
                return m_height;
            }
            set
            {
                m_height = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        internal float m_length;
        public float length
        {
            get
            {
                return m_length;
            }
            set
            {
                m_length = Mathf.Max(1, value);
            }
        }

        [SerializeField]
        private int m_meshBaseResolution;
        public int meshBaseResolution
        {
            get
            {
                return m_meshBaseResolution;
            }
            set
            {
                m_meshBaseResolution = Mathf.Clamp(value, 0, 10);
            }
        }

        [SerializeField]
        private int m_meshResolution;
        public int meshResolution
        {
            get
            {
                return m_meshResolution;
            }
            set
            {
                m_meshResolution = Mathf.Clamp(value, 0, 13);
            }
        }

        [SerializeField]
        private int m_chunkGridSize;
        public int chunkGridSize
        {
            get
            {
                return m_chunkGridSize;
            }
            set
            {
                m_chunkGridSize = Mathf.Max(1, value);
            }
        }
                
        public Vector3 size
        {
            get
            {
                return new Vector3(width, height, length);
            }
        }        

        public static MeshGenOptions Create()
        {
            MeshGenOptions o = new MeshGenOptions();
            o.width = 1000;
            o.height = 600;
            o.length = 1000;
            o.meshResolution = 12;
            o.meshBaseResolution = 6;
            o.chunkGridSize = 5;
            
            return o;
        }
    }
}

#endif