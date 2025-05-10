using UnityEngine;

[System.Serializable]
public class Item
{
    public int id;
    public string itemName;
    public string description;
    public Sprite icon;
    public int stackSize;
    public int maxStackSize;
    public bool isModifiable; // ← новое поле

    public Item(int id, string name, string description, Sprite icon, int stackSize, int maxStackSize, bool isModifiable = false)
    {
        this.id = id;
        this.itemName = name;
        this.description = description;
        this.icon = icon;
        this.stackSize = stackSize;
        this.maxStackSize = maxStackSize;
        this.isModifiable = isModifiable;
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
}
