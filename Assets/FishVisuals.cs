using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class FishVisuals : MonoBehaviour
    {
        private string _hashId;
        [SerializeField] Transform _scaleRoot; // used to scale the size of the fish;

        [SerializeField] float _weightMin;
        [SerializeField] float _weightMax;
        [SerializeField] float _scaleMin;
        [SerializeField] float _scaleMax;

        public void PrepareVisuals(string hashId)
        {
            _hashId = hashId;

        }

        public void GetHashId(float weight)
        {

        }
    }
}
