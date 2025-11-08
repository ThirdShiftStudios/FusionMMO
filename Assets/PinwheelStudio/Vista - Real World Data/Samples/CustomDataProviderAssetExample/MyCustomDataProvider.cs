#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;

namespace Pinwheel.Vista.RealWorldData.Sample
{
    //Mark this provider as Serializable so its properties will be displayed and saved in the Inspector
    [System.Serializable]
    public class MyCustomDataProvider : IRwdProvider
    {
        //Define the height range possible from this provider, will be used later for height remapping
        //The values usually come from the server, check your service description
        public const float MIN_HEIGHT = 0;
        public const float MAX_HEIGHT = 10000;

        public DataAvailability availability => DataAvailability.HeightMap; //Tell user what data this can provide, if there are multiple data type, use the | operator (ex: DataAvailability.HeightMap | DataAvailability.ColorMap)

        /// <summary>
        /// Implement the IRwdProvider.RequestHeightMap()
        /// This function do some work to retrieve the height map of the selected region
        /// </summary>
        /// <param name="gps"></param>
        /// <returns></returns>
        public DataRequest RequestHeightMap(GeoRect gps)
        {
            //Pass the request to a coroutine so it can run asynchronously
            ProgressHandler progress = new ProgressHandler(); 
            DataRequest request = new DataRequest(progress);
            CoroutineUtility.StartCoroutine(IRequestHeightMap(request, progress, gps));
            return request;
        }

        private IEnumerator IRequestHeightMap(DataRequest dataRequest, ProgressHandler progress, GeoRect gps)
        {
            progress.value = 0; //Set the progress value if you want
            yield return null;

            //Create a dummy 2x2 height map
            Vector2Int dataSize = new Vector2Int(2, 2);
            float[] dataArray = new float[4] { 0f, 0.2f, 0.67f, 1f }; //height data should be remapped to [0,1] range, using the MIN_HEIGHT and MAX_HEIGHT above
            progress.value = 0.5f;
            //For detail example of how to construct a request, contacting the server, downloading data, etc. please see the following files:
            //OpenTopographyDataProvider.cs
            //USGSDataProvider.cs
            yield return null;

            //After download and remap data, write them to the dataRequest
            //It's your responsibility to make sure dataSize and dataArray are matched
            dataRequest.heightMapData = dataArray;
            dataRequest.heightMapSize = dataSize;
            progress.value = 1f;

            //Mark the dataRequest as Complete so you won't block the operation
            dataRequest.Complete();
        }

        //Since this provider doesn't support Color Map, just throw an exception here
        public DataRequest RequestColorMap(GeoRect gps)
        {
            throw new System.NotImplementedException();
        }
    }
}
#endif
