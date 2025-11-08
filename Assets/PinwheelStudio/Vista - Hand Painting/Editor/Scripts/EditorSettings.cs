#if VISTA
using UnityEngine;

namespace Pinwheel.VistaEditor.HandPainting
{
    //[CreateAssetMenu(menuName ="Vista/Editor Settings (Hand Painting)", fileName ="EditorSettings_HandPainting")]
    public class EditorSettings : ScriptableObject
    {
        [System.Serializable]
        public class SimpleBrushSettings
        {
            [SerializeField]
            private float m_radius;
            public float radius
            {
                get
                {
                    return m_radius;
                }
                set
                {
                    m_radius = Mathf.Max(0, value);
                }
            }

            [SerializeField]
            private float m_falloff;
            public float falloff
            {
                get
                {
                    return m_falloff;
                }
                set
                {
                    m_falloff = Mathf.Clamp01(value);
                }
            }

            [SerializeField]
            private float m_opacity;
            public float opacity
            {
                get
                {
                    return m_opacity;
                }
                set
                {
                    m_opacity = Mathf.Clamp01(value);
                }
            }

            [SerializeField]
            private Color m_color;
            public Color color
            {
                get
                {
                    return m_color;
                }
                set
                {
                    m_color = value;
                }
            }

            public SimpleBrushSettings()
            {
                m_radius = 100;
                m_falloff = 0.5f;
                m_opacity = 1;
                m_color = Color.white;
            }

            public SimpleBrushSettings(float r, float f, float o)
            {
                m_radius = r;
                m_falloff = f;
                m_opacity = o;
                m_color = Color.white;
            }
        }

        [SerializeField]
        private SimpleBrushSettings m_biomeMaskBrushSettings;
        public SimpleBrushSettings biomeMaskBrushSettings
        {
            get
            {
                return m_biomeMaskBrushSettings;
            }
            set
            {
                m_biomeMaskBrushSettings = value;
            }
        }

        [SerializeField]
        private SimpleBrushSettings m_paintNodeBrushSettings;
        public SimpleBrushSettings paintNodeBrushSettings
        {
            get
            {
                return m_paintNodeBrushSettings;
            }
            set
            {
                m_paintNodeBrushSettings = value;
            }
        }

        private static EditorSettings s_instance;

        public void Reset()
        {
            m_biomeMaskBrushSettings = new SimpleBrushSettings();
            m_paintNodeBrushSettings = new SimpleBrushSettings(0.5f, 0.5f, 0.5f);
        }

        public static EditorSettings Get()
        {
            if (s_instance == null)
            {
                s_instance = Resources.Load<EditorSettings>("Vista/EditorSettings_HandPainting");
            }
            if (s_instance == null)
            {
                s_instance = ScriptableObject.CreateInstance<EditorSettings>();
                Debug.LogWarning("VISTA: Editor Settings asset does not exist. Please re-import the package.");
            }
            return s_instance;
        }
    }
}
#endif
