using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class LevelImageDisplay : MonoBehaviour
    {

        [Serializable]
        internal class LevelImageDisplayData
        {
            public int Level;
            public GameObject GameObject;
        }

        [SerializeField]
        LevelImageDisplayData[] _levelData;

        public void SetData(int level)
        {
            // Disable All
            foreach (var data in _levelData)
            {
                data.GameObject.SetActive(false);
            }

            foreach (var data in _levelData)
            {
                if(data.Level == level)
                    data.GameObject.SetActive(true);
            }
        }

    }


}
