using UnityEngine;
using SeaVibe.Interaction;

namespace SeaVibe.Inventory
{
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        public ItemData itemData;
        public int amount = 1;

        public string GetInteractionPrompt()
        {
            if (itemData != null)
                return $"Sebrat {itemData.itemName} ({amount}x)";
            return "Sebrat neznámý předmět";
        }

        public void OnInteract(GameObject interactor)
        {
            // Očekáváme, že hráč (interactor) má na sobě komponentu Inventory
            Inventory playerInventory = interactor.GetComponent<Inventory>();
            if (playerInventory != null && itemData != null)
            {
                int leftover = playerInventory.AddItem(itemData, amount);
                if (leftover == 0)
                {
                    Debug.Log($"Předmět {itemData.itemName} sebrán.");
                    playerInventory.PrintInventory(); // Pro otestování v konzoli
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("Inventář hráče je plný!");
                    amount = leftover; // Necháme na zemi to, co se nevešlo
                }
            }
        }
    }
}
