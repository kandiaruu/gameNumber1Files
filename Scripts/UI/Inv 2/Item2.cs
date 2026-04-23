using UnityEngine;

[System.Serializable]
public enum ItemOrientation
{
    Vertical,
    Horizontal
}

[System.Serializable]
public class Item2
{
    public int id;
    public string itemName;
    public Sprite icon;
    public int stackSize;
    public int maxStackSize;
    public int width; // Размер по ширине в базовой ориентации
    public int height; // Размер по высоте в базовой ориентации
    public bool canRotate; // Можно ли поворачивать предмет
    public ItemOrientation orientation; // Текущая ориентация

    public Item2(
        int id,
        string name,
        Sprite icon,
        int stackSize,
        int maxStackSize,
        int width,
        int height,
        bool canRotate,
        ItemOrientation orientation = ItemOrientation.Vertical)
    {
        this.id = id;
        this.itemName = name;
        this.icon = icon;
        this.stackSize = stackSize;
        this.maxStackSize = maxStackSize;
        this.width = width;
        this.height = height;
        this.canRotate = canRotate;
        this.orientation = orientation;
    }

    // Получить текущие размеры с учетом ориентации
    public (int width, int height) GetDimensions()
    {
        if (orientation == ItemOrientation.Horizontal && canRotate)
        {
            return (height, width); // Меняем местами ширину и высоту при горизонтальной ориентации
        }
        return (width, height);
    }

    public bool CanAddToStack(int amount)
    {
        return stackSize + amount <= maxStackSize;
    }

    public void AddToStack(int amount)
    {
        stackSize = Mathf.Min(stackSize + amount, maxStackSize);
    }

    public void RemoveFromStack(int amount)
    {
        stackSize = Mathf.Max(stackSize - amount, 0);
    }

    // Поворот предмета
    public void Rotate()
    {
        if (canRotate)
        {
            orientation = orientation == ItemOrientation.Vertical ? ItemOrientation.Horizontal : ItemOrientation.Vertical;
        }
    }
}