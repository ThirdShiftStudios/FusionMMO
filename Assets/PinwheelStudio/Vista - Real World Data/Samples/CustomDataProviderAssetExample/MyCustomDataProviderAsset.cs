#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;

namespace Pinwheel.Vista.RealWorldData.Sample
{
    //[CreateAssetMenu(menuName = "Vista/Data Provider (Custom Provider Example)")]
    public class MyCustomDataProviderAsset : DataProviderAsset
    {
        public override float minHeight => MyCustomDataProvider.MIN_HEIGHT; //The minimum height level (meters) of the whole dataset, not just the selected region
        public override float maxHeight => MyCustomDataProvider.MAX_HEIGHT; //The maximum height level (meters) of the whole dataset, not just the selected region

        [SerializeField]
        protected MyCustomDataProvider m_provider; //The actual data provider class which responsible for retrieving data from the server, don't forget to mark it as SerializeField so you can edit its properties in the Inspector
        public override IRwdProvider provider => m_provider;

        public override void Reset()
        {
            base.Reset();
            m_provider = new MyCustomDataProvider();
        }

        /// <summary>
        /// Called when the height map need to be updated (clicked on the Download button, called by Real World Biome, etc)
        /// </summary>
        /// <param name="gps">The selected region in GPS spatial reference (4236)</param>
        /// <returns></returns>
        public override DataRequest RequestHeightMap(GeoRect gps)
        {
            //Most of the time you just call the RequestHeightMap() function on the provider class
            //You can setup the provider additional 'non-serializable' parameters such as API key at this step
            return m_provider.RequestHeightMap(gps);
        }

        /// <summary>
        /// Called when the color map (satellite image) need to be updated (clicked on the Download button, called by Real World Biome, etc)
        /// </summary>
        /// <param name="gps">The selected region in GPS spatial reference (4236)</param>
        /// <returns></returns>
        public override DataRequest RequestColorMap(GeoRect gps)
        {
            //You can call the RequestColorMap() on the provider here
            //But since MyCustomDataProvider doesn't provide color map, the request should be done immediately with no data
            return DataRequest.DoneAndEmpty();
        }
    }
}
#endif
