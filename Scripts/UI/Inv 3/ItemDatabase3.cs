//
// ScriptableObject that acts as the central item registry. Stores an array of
// ItemData definitions (id, name, description, icon, stackability, categories)
// and provides a lookup method that constructs a runtime Item3 instance by ID.
//

using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase3", menuName = "Inventory3/ItemDatabase3")]
public class ItemDatabase3 : ScriptableObject
{
    [System.Serializable]
    public class ItemData
    {
        public int id;
        public string itemName;
        public string description;
        public Sprite icon;
        public bool isStackable;
        public string[] categories;
    }

    public ItemData[] items;

    // Finds an item definition by ID and returns a new Item3 runtime instance, or logs an error and returns null if not found
    public Item3 GetItemById(int id, int stackSize = 1)
    {
        foreach (var itemData in items)
        {
            if (itemData.id == id)
            {
                return new Item3(
                    id: itemData.id,
                    name: itemData.itemName,
                    icon: itemData.icon,
                    stackSize: stackSize,
                    stack: itemData.isStackable,
                    categories: itemData.categories
                );
            }
        }
        Debug.LogError($"Item with ID {id} not found!");
        return null;
    }
}
