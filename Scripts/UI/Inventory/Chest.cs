using System.Collections.Generic;
using UnityEngine;
public enum ChestSize
{
    Small,
    Large
}
public class Chest : MonoBehaviour

{
    public ChestSize size = ChestSize.Small; 
    public List<ChestItemEntry> chestItems = new List<ChestItemEntry>();
    public void Interact(InventoryPanel inventoryPanel)
    {
        inventoryPanel.LoadChestItems(chestItems);
    }

    public int GetSlotsCount() => size == ChestSize.Small ? 12 : 24;

    public void SaveChestItems(List<ChestItemEntry> entries)
    {
        chestItems = new List<ChestItemEntry>(entries); // просто копируем список
    }

    public void FillRandomly(int slotsCount)
    {
        chestItems.Clear();
        for (int i = 0; i < slotsCount; i++)
        {
            if (Random.value <= 0.1f)
            {
                int stackSize = GetRandomStackSize();
                chestItems.Add(new ChestItemEntry
                {
                    slotIndex = i,
                    itemId = 0,
                    stackSize = stackSize
                });
            }
        }
    }

    private int GetRandomStackSize()
    {
        // 1/3 шанс на 1, 1/3 на 2, 1/3 на 3
        return Random.Range(1, 4);
    }

    void Start()
    {
        FillRandomly(GetSlotsCount()); // или нужное количество слотов
    }
}
