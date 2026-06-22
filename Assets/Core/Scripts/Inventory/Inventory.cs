using System.Collections.Generic;
using UnityEngine;

namespace SeaVibe.Inventory
{
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int amount;

        public InventorySlot(ItemData item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }
    }

    public class Inventory : MonoBehaviour
    {
        public List<InventorySlot> slots = new List<InventorySlot>();

        // Přidá předmět do inventáře, vrací zbytek, který se nevešel
        public int AddItem(ItemData itemToAdd, int amountToAdd)
        {
            if (itemToAdd.isStackable)
            {
                // Najdi existující slot se stejným předmětem, který není plný
                foreach (var slot in slots)
                {
                    if (slot.item == itemToAdd && slot.amount < itemToAdd.maxStackSize)
                    {
                        int spaceLeft = itemToAdd.maxStackSize - slot.amount;
                        if (amountToAdd <= spaceLeft)
                        {
                            slot.amount += amountToAdd;
                            return 0; // Vše se vešlo
                        }
                        else
                        {
                            slot.amount += spaceLeft;
                            amountToAdd -= spaceLeft;
                        }
                    }
                }
            }

            // Pokud ještě zbývá amountToAdd, vytvoříme nové sloty
            while (amountToAdd > 0)
            {
                int amountForNewSlot = Mathf.Min(amountToAdd, itemToAdd.maxStackSize);
                slots.Add(new InventorySlot(itemToAdd, amountForNewSlot));
                amountToAdd -= amountForNewSlot;
            }

            return amountToAdd;
        }

        // Odebere daný počet předmětů
        public bool RemoveItem(ItemData itemToRemove, int amountToRemove)
        {
            int totalAvailable = 0;
            foreach (var slot in slots)
            {
                if (slot.item == itemToRemove)
                    totalAvailable += slot.amount;
            }

            if (totalAvailable < amountToRemove)
                return false; // Nemáme dostatek

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].item == itemToRemove)
                {
                    if (slots[i].amount >= amountToRemove)
                    {
                        slots[i].amount -= amountToRemove;
                        if (slots[i].amount == 0) slots.RemoveAt(i);
                        break;
                    }
                    else
                    {
                        amountToRemove -= slots[i].amount;
                        slots.RemoveAt(i);
                    }
                }
            }
            return true;
        }

        public void PrintInventory()
        {
            Debug.Log($"--- Obsah Inventáře: {gameObject.name} ---");
            foreach (var slot in slots)
            {
                Debug.Log($"{slot.item.itemName}: {slot.amount}x");
            }
        }
    }
}
