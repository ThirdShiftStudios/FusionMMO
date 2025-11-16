using Fusion.Addons.AnimationController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class BeerDrinkState : ClipState
    {
        [SerializeField, Range(0f, 1f)]
        private float _buffApplyNormalizedTime = 1f;

        public float BuffApplyNormalizedTime => Mathf.Clamp01(_buffApplyNormalizedTime);
    }
}
