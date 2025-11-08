#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Pinwheel.Vista.NavMeshUtilities
{
    public static class Utilities
    {
        public static T[] To1dArray<T>(T[][] jaggedArray)
        {
            List<T> result = new List<T>();
            for (int z = 0; z < jaggedArray.Length; ++z)
            {
                for (int x = 0; x < jaggedArray[z].Length; ++x)
                {
                    result.Add(jaggedArray[z][x]);
                }
            }
            return result.ToArray();
        }

        public static T[] To1dArray<T>(T[,] grid)
        {
            int height = grid.GetLength(0);
            int width = grid.GetLength(1);
            T[] result = new T[height * width];
            for (int z = 0; z < height; ++z)
            {
                for (int x = 0; x < width; ++x)
                {
                    result[To1DIndex(x, z, width)] = grid[z, x];
                }
            }
            return result;
        }

        public static void Fill<T>(NativeArray<T> array, T value) where T : struct
        {
            int length = array.Length;
            for (int i = 0; i < length; ++i)
            {
                array[i] = value;
            }
        }

        public static void Fill<T>(T[] array, T value)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = value;
            }
        }

        public static void CopyTo<T>(T[] src, T[] des)
        {
            int limit = Mathf.Min(src.Length, des.Length);
            for (int i = 0; i < limit; ++i)
            {
                des[i] = src[i];
            }
        }

        public static int To1DIndex(int x, int z, int width)
        {
            return z * width + x;
        }
        public static Transform GetChildrenWithName(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                GameObject g = new GameObject(name);
                g.transform.parent = parent;
                ResetTransform(g.transform, parent);
                t = g.transform;
            }
            return t;
        }
        public static void ResetTransform(Transform t, Transform parent)
        {
            t.parent = parent;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
        public static void DestroyGameobject(GameObject g)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                GameObject.Destroy(g);
            else
                GameObject.DestroyImmediate(g);
#else
            GameObject.Destroy(g);
#endif
        }
        public static void DestroyObject(Object o)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                Object.Destroy(o);
            else
                Object.DestroyImmediate(o, true);
#else
            GameObject.Destroy(o);
#endif
        }
        public static bool EnsureArrayLength<T>(ref T[] array, int count)
        {
            if (array == null || array.Length != count)
            {
                array = new T[count];
                return false;
            }
            return true;
        }
        public static void ClearChildren(Transform t)
        {
            int childCount = t.childCount;
            for (int i = childCount - 1; i >= 0; --i)
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    GameObject.DestroyImmediate(t.GetChild(i).gameObject);
                }
                else
                {
                    GameObject.Destroy(t.GetChild(i).gameObject);
                }
#else
                GameObject.Destroy(t.GetChild(i).gameObject);
#endif
            }
        }
        public static void MarkCurrentSceneDirty()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
        }
        public static Gradient Clone(Gradient src)
        {
            if (src == null)
                return null;
            Gradient des = new Gradient();
            des.SetKeys(src.colorKeys, src.alphaKeys);
            return des;
        }

        public static AnimationCurve Clone(AnimationCurve src)
        {
            if (src == null)
                return null;
            AnimationCurve des = new AnimationCurve();
            Keyframe[] keys = src.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                des.AddKey(keys[i]);
            }

            des.preWrapMode = src.preWrapMode;
            des.postWrapMode = src.postWrapMode;
            return des;
        }
        public static float InverseLerpUnclamped(float a, float b, float value)
        {
            if (a != b)
            {
                return (value - a) / (b - a);
            }
            return 0f;
        }
    }
}

#endif