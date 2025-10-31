using UnityEngine;

namespace TPSBR
{
    public readonly struct InteractionContext
    {
        public InteractionContext(Interactions interactions, GameObject interactor, Agent agent, Character character,
            Inventory inventory, CharacterAnimationController animationController)
        {
            Interactions = interactions;
            Interactor = interactor;
            Agent = agent;
            Character = character;
            Inventory = inventory;
            AnimationController = animationController;
        }

        public Interactions Interactions { get; }
        public GameObject Interactor { get; }
        public Agent Agent { get; }
        public Character Character { get; }
        public Inventory Inventory { get; }
        public CharacterAnimationController AnimationController { get; }
    }

    public interface IInteraction
    {
        public string Name { get; }
        public string Description { get; }
        public Vector3 HUDPosition { get; }
        public bool IsActive { get; }
        public bool Interact(in InteractionContext context, out string message);
    }

    public interface IPickup : IInteraction
    {
    }
}
