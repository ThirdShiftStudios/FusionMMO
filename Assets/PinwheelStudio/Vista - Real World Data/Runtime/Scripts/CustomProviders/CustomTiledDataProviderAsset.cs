#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;

namespace Pinwheel.Vista.RealWorldData
{
    [PreferBinarySerialization]
    [CreateAssetMenu(menuName = "Vista/Data Provider (Custom Tile Based)")]
    [HelpURL("https://docs.google.com/document/d/1zRDVjqaGY2kh4VXFut91oiyVCUex0OV5lTUzzCSwxcY/edit#heading=h.hnvzbw82mxif")]
    public class CustomTiledDataProviderAsset : DataProviderAsset
    {
        public override float minHeight => 0;
        public override float maxHeight => 0;

        [SerializeField]
        protected CustomTiledDataProvider m_provider;
        public override IRwdProvider provider => m_provider;

        [SerializeField]
        protected TextAsset m_apiKeyAsset;
        public TextAsset apiKeyAsset
        {
            get
            {
                return m_apiKeyAsset;
            }
            set
            {
                m_apiKeyAsset = value;
            }
        }

        private void OnValidate()
        {
            if (m_provider != null)
            {
                m_provider.apiKey = m_apiKeyAsset != null ? m_apiKeyAsset.text : string.Empty;
                m_provider.OnValidate();
            }
        }

        public override void Reset()
        {
            base.Reset();
            m_provider = new CustomTiledDataProvider();
        }

        public override DataRequest RequestColorMap(GeoRect gps)
        {
            m_provider.apiKey = m_apiKeyAsset != null ? m_apiKeyAsset.text : string.Empty;
            return m_provider.RequestColorMap(gps);
        }
    }
}
#endif
