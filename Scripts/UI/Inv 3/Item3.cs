using UnityEngine;

[System.Serializable]
public class Item3
{
    public int id;
    public string itemName;
    public Sprite icon;
    public int stackSize;
    public bool isStackable;
    public string[] categories;

    public Item3(int id, string name, Sprite icon, int stackSize, bool stack, string[] categories = null)
    {
        this.id = id;
        this.itemName = name;
        this.icon = icon;
        this.stackSize = stackSize;
        this.isStackable = stack; // ВАЖНО: сохраняем флаг стака
        this.categories = categories;
    }
}