using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    public List<ChestItemEntry> chestItems = new List<ChestItemEntry>();
    public void Interact(InventoryPanel inventoryPanel)
    {
        inventoryPanel.LoadChestItems(chestItems);
    }

    public void SaveChestItems(List<ChestItemEntry> entries)
    {
        chestItems = new List<ChestItemEntry>(entries); // просто копируем список
    }

    public void FillRandomly(int slotsCount = 12)
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
        FillRandomly(slotsCount: 12); // или нужное количество слотов
    }
}
