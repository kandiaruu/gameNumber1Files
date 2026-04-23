using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase2", menuName = "Inventory2/Item Database2")]
public class ItemDatabase2 : ScriptableObject
{
    [System.Serializable]
    public class ItemData
    {
        public int id;
        public string itemName;
        public string description;
        public Sprite icon;
        public int maxStackSize;
        public int width; // ← Новое поле для ширины
        public int height; // ← Новое поле для высоты
        public bool canRotate; // ← Новое поле для возможности поворота
        public ItemOrientation orientation; // ← Новое поле для ориентации
    }

    public ItemData[] items;

    public Item2 GetItemById(int id, int stackSize = 1)
    {
        foreach (var itemData in items)
        {
            if (itemData.id == id)
            {
                return new Item2(
                    id: itemData.id,
                    name: itemData.itemName,
                    icon: itemData.icon,
                    stackSize: stackSize,
                    maxStackSize: itemData.maxStackSize,
                    width: itemData.width,
                    height: itemData.height,
                    canRotate: itemData.canRotate,
                    orientation: itemData.orientation
                );
            }
        }
        Debug.LogError($"Предмет с ID {id} не найден!");
        return null;
    }
}