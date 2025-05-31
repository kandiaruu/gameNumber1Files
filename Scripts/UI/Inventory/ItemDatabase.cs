using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class ItemData
    {
        public int id;
        public string itemName;
        public string description;
        public Sprite icon;
        public int maxStackSize;
        public bool isModifiable; // ← новое поле
        public bool isRecipe;                // новое поле
        public int recipeUsesLeft;          // новое поле (null или -1 = бесконечный)
    }

    public ItemData[] items;

    public Item GetItemById(int id, int stackSize = 1)
    {
        foreach (var itemData in items)
        {
            if (itemData.id == id)
            {
                return new Item(
                    itemData.id,
                    itemData.itemName,
                    itemData.description,
                    itemData.icon,
                    stackSize,
                    itemData.maxStackSize,
                    itemData.isModifiable,
                    itemData.isRecipe,
                    itemData.recipeUsesLeft
                );
            }
        }
        Debug.LogError($"Предмет с ID {id} не найден!");
        return null;
    }
}