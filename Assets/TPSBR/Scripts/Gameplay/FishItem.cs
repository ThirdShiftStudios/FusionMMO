using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class FishItem : ContextBehaviour
    {
        public enum FishState
        {
            Idle,
            Fighting,
            MovingAroundLure,
            Caught,
        }

        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private Transform _visuals;

        [Networked]
        public FishState State { get; set; }

        public Animator Animator => _animator;
        public Transform Visuals => _visuals;
    }
}
