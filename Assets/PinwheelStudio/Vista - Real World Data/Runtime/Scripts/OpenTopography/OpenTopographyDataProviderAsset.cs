#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

namespace Pinwheel.Vista.RealWorldData
{
    [PreferBinarySerialization]
    [CreateAssetMenu(menuName = "Vista/Data Provider (Open Topography)")]
    [HelpURL("https://docs.google.com/document/d/1zRDVjqaGY2kh4VXFut91oiyVCUex0OV5lTUzzCSwxcY/edit#heading=h.hnvzbw82mxif")]
    public class OpenTopographyDataProviderAsset : DataProviderAsset
    {
        [SerializeField]
        private OpenTopographyDataProvider m_provider;
        public override IRwdProvider provider
        {
            get
            {
                return m_provider;
            }
        }

        [SerializeField]
        private TextAsset m_apiKeyAsset;
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

        public override float minHeight => OpenTopographyDataProvider.MIN_HEIGHT;
        public override float maxHeight => OpenTopographyDataProvider.MAX_HEIGHT;

        private void OnValidate()
        {
            if (m_provider != null)
            {
                m_provider.OnValidate();
            }
        }

        public override void Reset()
        {
            base.Reset();

            m_longLat = GpsUtils.VIETNAM_TAXUA;
            m_provider = new OpenTopographyDataProvider();
        }

        public override DataRequest RequestHeightMap(GeoRect gps)
        {
            m_provider.apiKey = apiKeyAsset != null ? apiKeyAsset.text : string.Empty;
            return m_provider.RequestHeightMap(gps);
        }
    }
}
#endif
