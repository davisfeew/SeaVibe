using UnityEngine;

namespace SeaVibe.Inventory
{
    [CreateAssetMenu(fileName = "New Item", menuName = "SeaVibe/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Item Info")]
        public string itemName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Stacking")]
        public bool isStackable = true;
        public int maxStackSize = 99;
    }
}
