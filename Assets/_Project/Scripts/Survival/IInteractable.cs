public interface IInteractable
{
    string InteractionPrompt { get; }
    bool CanInteract(FirstPersonController player);
    void Interact(FirstPersonController player);
}
