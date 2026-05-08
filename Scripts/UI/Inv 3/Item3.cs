//
// Serializable runtime representation of an item definition. Holds the item's
// identifier, display name, icon sprite, stack size, stackability flag, and an
// optional array of category tags used for filtering in the inventory panel.
//

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

    // Constructs a new Item3 with all fields explicitly provided
    public Item3(int id, string name, Sprite icon, int stackSize, bool stack, string[] categories = null)
    {
        this.id = id;
        this.itemName = name;
        this.icon = icon;
        this.stackSize = stackSize;
        this.isStackable = stack;
        this.categories = categories;
    }
}
