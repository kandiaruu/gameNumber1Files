using System.Collections.Generic;

[System.Serializable]
public class StashData
{
    public List<ChestItemEntry> items = new List<ChestItemEntry>();
    public string stashName;
    public int stashSlots;
    public int stashColumns;

    public StashData(string name, int slots, int columns)
    {
        stashName = name;
        stashSlots = slots;
        stashColumns = columns;
    }
}