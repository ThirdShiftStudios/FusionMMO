#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using System;

namespace Pinwheel.Vista.NavMeshUtilities
{
    [ExecuteInEditMode]
    public class DemoNavAreaCallbackListener : MonoBehaviour
    {        
        public void OnEnable()
        {
            NavAreaMeshGenerator.spawnNavAreaCallback += OnSpawnNavArea;
        }

        public void OnDisable()
        {
            NavAreaMeshGenerator.spawnNavAreaCallback -= OnSpawnNavArea;
        }

        private void OnSpawnNavArea(NavAreaMeshGenerator sender, GameObject navAreaObject)
        {
            Debug.Log($"Spawning nav area object: {navAreaObject.name}");            
        }
    }
}
#endif
