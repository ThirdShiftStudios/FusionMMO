#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;
using UnityEditor;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public static class MapBookmarks
    {
        [System.Serializable]
        public struct Bookmark
        {
            public string name;
            public GeoRect coordinates;

            public Bookmark(string n, GeoRect r)
            {
                name = n;
                coordinates = r;
            }
        }

        [System.Serializable]
        private class BookmarkCollection
        {
            public List<Bookmark> bookmarks = new List<Bookmark>();
        }

        private const string PREF_KEY = "vista-map-bookmarks";
        private static BookmarkCollection m_bookmarks;

        public static List<Bookmark> GetAll()
        {
            if (m_bookmarks == null)
            {
                Load();
            }
            return new List<Bookmark>(m_bookmarks.bookmarks);
        }

        private static void Load()
        {
            string json = EditorPrefs.GetString(PREF_KEY, null);
            if (!string.IsNullOrEmpty(json))
            {
                m_bookmarks = new BookmarkCollection();
                EditorJsonUtility.FromJsonOverwrite(json, m_bookmarks);
            }
            else
            {
                m_bookmarks = new BookmarkCollection();
            }
        }

        public static void Add(string name, GeoRect coordinates)
        {
            if (m_bookmarks == null)
            {
                Load();
            }
            m_bookmarks.bookmarks.RemoveAll(b => string.Equals(b.name, name));
            m_bookmarks.bookmarks.Add(new Bookmark(name, coordinates));
        }

        public static void Save()
        {
            if (m_bookmarks == null)
                return;
            string json = EditorJsonUtility.ToJson(m_bookmarks);
            EditorPrefs.SetString(PREF_KEY, json);
        }

        public static bool HasBookmarks()
        {
            if (m_bookmarks == null)
            {
                Load();
            }
            return m_bookmarks.bookmarks.Count > 0;
        }

        [MenuItem("Window/Vista/Real World Data/Clear Map Bookmarks")]
        public static void ClearAll()
        {
            m_bookmarks = null;
            EditorPrefs.DeleteKey(PREF_KEY);
        }
    }
}
#endif
