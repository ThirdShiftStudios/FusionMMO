#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

namespace Pinwheel.Vista.BigWorld
{
    public static class Constants
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void OnInitialize()
        {
            Pinwheel.Vista.Constants.getResMaxCallback += OnGetResMax;
            Pinwheel.Vista.Constants.getHmResMaxCallback += OnGetHmResMax;
        }

        public static readonly int K4096 = 4096;
        public static readonly int K4097 = 4097;

        private static int OnGetResMax()
        {
            return K4096;
        }

        private static int OnGetHmResMax()
        {
            return K4097;
        }
    }
}
#endif
