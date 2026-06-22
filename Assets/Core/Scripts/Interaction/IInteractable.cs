namespace SeaVibe.Interaction
{
    public interface IInteractable
    {
        string GetInteractionPrompt();
        void OnInteract(UnityEngine.GameObject interactor);
    }
}
