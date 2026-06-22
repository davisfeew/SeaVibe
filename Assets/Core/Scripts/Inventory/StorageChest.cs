using UnityEngine;
using SeaVibe.Interaction;

namespace SeaVibe.Inventory
{
    [RequireComponent(typeof(Inventory))]
    public class StorageChest : MonoBehaviour, IInteractable
    {
        private Inventory _chestInventory;

        private void Awake()
        {
            _chestInventory = GetComponent<Inventory>();
        }

        public string GetInteractionPrompt()
        {
            return "Otevřít Truhlu";
        }

        public void OnInteract(GameObject interactor)
        {
            Inventory playerInventory = interactor.GetComponent<Inventory>();
            if (playerInventory != null)
            {
                // Prozatím jen vypíšeme obsah obou inventářů do konzole
                Debug.Log("=== OTEVŘENA TRUHLA ===");
                playerInventory.PrintInventory();
                _chestInventory.PrintInventory();
                
                // TODO: Otevření reálného UI pro přesun předmětů
            }
        }
    }
}
