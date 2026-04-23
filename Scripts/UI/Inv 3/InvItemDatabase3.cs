[System.Serializable]
public class InvItemDatabase3
{
    public string instanceId;     // уникальный ID конкретного предмета
    public int itemId;
    public int stackSize;
    public long firstAddedTicks;
    public long lastAddedTicks;
    public bool isEquipped;
}