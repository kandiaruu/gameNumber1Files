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
        Debug.LogError($"Предмет с ID {id} не найден!");
        return null;
    }
}