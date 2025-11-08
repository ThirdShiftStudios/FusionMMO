#if VISTA
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Pinwheel.VistaEditor.Splines
{
    [InitializeOnLoad]
    public static class ModuleInitializer
    {
        public static bool isUnitySplineInstalled { get; set; }
        public static bool isCurvySplineInstalled { get; set; }
        public static bool isYasirkulaSplineInstalled { get; set; }

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            ResetPackageState();
            CheckPackagesInstalled();
            SetScriptingSymbols();
        }

        private static void ResetPackageState()
        {
            isUnitySplineInstalled = false;
            isCurvySplineInstalled = false;
            isYasirkulaSplineInstalled = false;
        }

        private static void CheckPackagesInstalled()
        {
            List<Type> loadedTypes = GetAllLoadedTypes();
            foreach (Type t in loadedTypes)
            {
                if (!string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("UnityEngine.Splines"))
                {
                    isUnitySplineInstalled = true;
                }
                if (!string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("FluffyUnderware.Curvy"))
                {
                    isCurvySplineInstalled = true;
                }
                if (!string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("BezierSolution"))
                {
                    isYasirkulaSplineInstalled = true;
                }
            }
        }

        private static List<Type> GetAllLoadedTypes()
        {
            List<Type> loadedTypes = new List<Type>();
            List<string> typeName = new List<string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.IsVisible && !t.IsGenericType)
                    {
                        typeName.Add(t.Name);
                        loadedTypes.Add(t);
                    }
                }
            }
            return loadedTypes;
        }

        private static void SetScriptingSymbols()
        {
            List<string> lines = new List<string>();
            lines.Add("-define:VISTA_SPLINES");
            if (isUnitySplineInstalled)
            {
                lines.Add("-define:VISTA_UNITY_SPLINE");
            }
            if (isCurvySplineInstalled)
            {
                lines.Add("-define:VISTA_CURVY_SPLINE");
            }
            if (isYasirkulaSplineInstalled)
            {
                lines.Add("-define:VISTA_YASIRKULA_SPLINE");
            }

            string cscrsp = "csc.rsp";
            string[] guid;
            guid = AssetDatabase.FindAssets("l:VistaSplinesDefines");
            for (int i = 0; i < guid.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid[i]);
                string fileName = Path.GetFileName(path);
                if (string.Equals(fileName, cscrsp))
                {
                    File.WriteAllLines(path, lines);
                }
            }
        }
    }
}
#endif