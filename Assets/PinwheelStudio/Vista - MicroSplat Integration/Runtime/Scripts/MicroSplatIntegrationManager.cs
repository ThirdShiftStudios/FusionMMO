#if VISTA
#if __MICROSPLAT__
using JBooth.MicroSplat;
using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Vista.MicroSplatIntegration
{
    [AddComponentMenu("Vista/MicroSplat Integration Manager")]
    [RequireComponent(typeof(VistaManager))]
    [ExecuteInEditMode]
    public class MicroSplatIntegrationManager : MonoBehaviour
    {
        public delegate void UpdateTextureArraysHandler(MicroSplatIntegrationManager sender);
        public static event UpdateTextureArraysHandler updateTextureArrayCallback;

        public delegate void UpdateFxMapHandler(MicroSplatIntegrationManager sender, ITile tile, List<string> textureLabels, List<RenderTexture> textures);
        public static event UpdateFxMapHandler updateFxMapCallback;

        public delegate void FinishingUpHandler(MicroSplatIntegrationManager sender);
        public static event FinishingUpHandler finishingUpCallback;

        [SerializeField]
        protected VistaManager m_vistaManager;
        public VistaManager vistaManager
        {
            get
            {
                return m_vistaManager;
            }
        }

        [SerializeField]
        protected TextureArrayConfig m_textureArrayConfig;
        public TextureArrayConfig textureArrayConfig
        {
            get
            {
                return m_textureArrayConfig;
            }
            set
            {
                m_textureArrayConfig = value;
            }
        }

        [SerializeField]
        protected bool m_updateTextureArraysAfterGenerating;
        public bool updateTextureArraysAfterGenerating
        {
            get
            {
                return m_updateTextureArraysAfterGenerating;
            }
            set
            {
                m_updateTextureArraysAfterGenerating = value;
            }
        }

        [SerializeField]
        protected string m_fxMapDirectory;
        public string fxMapDirectory
        {
            get
            {
                return m_fxMapDirectory;
            }
            set
            {
                m_fxMapDirectory = value;
            }
        }

        [SerializeField]
        protected bool m_updateFxMapsAfterGenerating;
        public bool updateFxMapsAfterGenerating
        {
            get
            {
                return m_updateFxMapsAfterGenerating;
            }
            set
            {
                m_updateFxMapsAfterGenerating = value;
            }
        }

        protected void Reset()
        {
            m_updateTextureArraysAfterGenerating = true;
            m_updateFxMapsAfterGenerating = true;
            m_fxMapDirectory = "Assets/";
        }

        protected void OnEnable()
        {
            m_vistaManager = GetComponent<VistaManager>();
            VistaManager.afterGenerating += OnAfterGenerating;
            VistaManager.genericTexturesPopulated += OnGenericTexturesPopulated;
        }

        protected void OnDisable()
        {
            VistaManager.afterGenerating -= OnAfterGenerating;
            VistaManager.genericTexturesPopulated -= OnGenericTexturesPopulated;
        }

        private void OnAfterGenerating(VistaManager sender)
        {
            if (m_vistaManager != sender)
                return;

            if (updateTextureArraysAfterGenerating)
            {
                updateTextureArrayCallback?.Invoke(this);
            }

            finishingUpCallback?.Invoke(this);
        }

        private void OnGenericTexturesPopulated(VistaManager sender, ITile tile, List<string> labels, List<RenderTexture> textures)
        {
            updateFxMapCallback?.Invoke(this, tile, labels, textures);
        }
    }
}
#endif
#endif