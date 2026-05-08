public interface IInteractable
{
    bool CanInteract { get; }
    string GetPromptText(); // e.g. "Pick up receiver"
    void Interact();
}