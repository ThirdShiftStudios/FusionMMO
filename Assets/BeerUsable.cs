using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class BeerUsable : Weapon
    {
        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {

        }

       
    }
}
