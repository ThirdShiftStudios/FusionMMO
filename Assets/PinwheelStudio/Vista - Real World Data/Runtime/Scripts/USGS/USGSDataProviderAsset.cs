#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

namespace Pinwheel.Vista.RealWorldData.USGS
{
    [PreferBinarySerialization]
    [CreateAssetMenu(menuName = "Vista/Data Provider (USGS)")]
    [HelpURL("https://docs.google.com/document/d/1zRDVjqaGY2kh4VXFut91oiyVCUex0OV5lTUzzCSwxcY/edit#heading=h.hnvzbw82mxif")]
    public class USGSDataProviderAsset : DataProviderAsset
    {
        public override float minHeight => USGSDataProvider.MIN_HEIGHT;
        public override float maxHeight => USGSDataProvider.MAX_HEIGHT;

        [SerializeField]
        private USGSDataProvider m_provider;
        public override IRwdProvider provider
        {
            get
            {
                return m_provider;
            }
        }

        public override void Reset()
        {
            base.Reset();
            m_provider = new USGSDataProvider();
            m_longLat = GpsUtils.USA;
        }

        public override ProgressiveTask RequestAndSaveAll()
        {
            ProgressiveTask task = new ProgressiveTask();
            CoroutineUtility.StartCoroutine(IRequestAndSaveAll(task));
            return task;
        }

        private IEnumerator IRequestAndSaveAll(ProgressiveTask task)
        {
            if (m_provider.downloadHeightMap)
                yield return RequestAndSaveHeightMap();
            if (m_provider.downloadColorMap)
                yield return RequestAndSaveColorMap();
            task.Complete();
        }

        public override DataRequest RequestHeightMap(GeoRect gps)
        {
            return m_provider.RequestHeightMap(gps);
        }

        public override DataRequest RequestColorMap(GeoRect gps)
        {
            return m_provider.RequestColorMap(gps);
        }
    }
}
#endif
