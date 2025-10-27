using Fusion;
using TSS.Data;
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
        private Transform _hookPlacement;
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private FishVisuals _fishVisuals;

        [Networked]
        public FishState State { get; set; }

        [Networked]
        public int DefinitionId { get; set; }

        public Animator Animator => _animator;
        public FishVisuals FishVisuals => _fishVisuals;
        public Transform HookPlacement => _hookPlacement;
        public FishDefinition Definition => ItemDefinition.Get(DefinitionId) as FishDefinition;
    }
}
