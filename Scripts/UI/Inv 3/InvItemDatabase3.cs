//
// Serializable data record that represents a single item entry in the player's
// inventory. Stores the unique instance ID, item type ID, stack size, acquisition
// timestamps, and whether the item is currently equipped.
//

[System.Serializable]
public class InvItemDatabase3
{
    public string instanceId;
    public int itemId;
    public int stackSize;
    public long firstAddedTicks;
    public long lastAddedTicks;
    public bool isEquipped;
}
