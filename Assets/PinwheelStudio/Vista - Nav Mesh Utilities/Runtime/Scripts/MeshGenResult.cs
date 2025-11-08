#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Pinwheel.Vista.NavMeshUtilities
{
    /// <summary>
    /// Contains generated data of the conversion pipeline, including terrain meshes, textures, material, etc.
    /// </summary>
    public class MeshGenResult : System.IDisposable
    {

        private Dictionary<Vector3Int, Mesh> meshes;

        public long ProcessingTimeMiliSec { get; internal set; }

        internal MeshGenResult()
        {
            meshes = new Dictionary<Vector3Int, Mesh>();
        }

        internal void AddMesh(Mesh mesh, int indexX, int indexY, int lod)
        {
            meshes.Add(new Vector3Int(indexX, indexY, lod), mesh);
        }

        public Mesh GetMesh(int indexX, int indexY, int lod)
        {
            Mesh res;
            if (meshes.TryGetValue(new Vector3Int(indexX, indexY, lod), out res))
            {
                return res;
            }
            else
            {
                return null;
            }
        }

        public void Validate()
        {
            foreach (Mesh m in meshes.Values)
            {
                if (m == null)
                    continue;
                m.Optimize();
                if (m.vertexCount == 0)
                {
                    Object.DestroyImmediate(m);
                }
            }
        }

        public void Dispose()
        {
            if (meshes != null)
            {
                foreach (Mesh mesh in meshes.Values)
                {
                    if (mesh == null)
                        continue;
                    Object.DestroyImmediate(mesh, true);
                }
            }
        }
    }
}

#endif