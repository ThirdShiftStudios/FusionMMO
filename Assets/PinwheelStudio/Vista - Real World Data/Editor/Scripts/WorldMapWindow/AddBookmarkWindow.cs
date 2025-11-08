#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Vista.RealWorldData;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public class AddBookmarkWindow : EditorWindow
    {
        private static readonly GUIContent HEADER = new GUIContent("Add Bookmark");
        private static readonly GUIContent ADD = new GUIContent("Add");

        private static readonly Vector2 DEFAULT_WINDOW_SIZE = new Vector2(300, 75);

        private string bookmarkName;
        private GeoRect coordinates;

        public static void Show(GeoRect coords, Rect buttonRect)
        {
            AddBookmarkWindow w = AddBookmarkWindow.CreateInstance<AddBookmarkWindow>();
            w.coordinates = coords;
            w.ShowAsDropDown(buttonRect, DEFAULT_WINDOW_SIZE);
        }

        public void OnGUI()
        {
            EditorCommon.Header(HEADER);
            bookmarkName = EditorGUILayout.TextField(bookmarkName);
            if (GUILayout.Button(ADD))
            {
                MapBookmarks.Add(bookmarkName, coordinates);
                Debug.Log($"Adding map bookmark: {bookmarkName} {coordinates}");
                MapBookmarks.Save();
                Close();
            }
        }
    }
}
#endif
