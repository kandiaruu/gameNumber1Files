using UnityEngine;
using System.Collections.Generic;
public enum InventoryPanelType
{
    Player,
    Chest,
}
public class InventoryPanel : MonoBehaviour, IInventoryPanel, IChestPanel
{
    [SerializeField] private InventoryPanelType panelType;
    public InventoryPanelType PanelType => panelType; // Геттер, если нужен доступ снаружи
    [SerializeField] public List<InventorySlot> slots = new List<InventorySlot>();
    [SerializeField] private ItemDatabase itemDatabase; // Ссылка на базу данных предметов

    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        slots.Clear();
        foreach (Transform child in transform)
        {
            var slot = child.GetComponent<InventorySlot>();
            if (slot != null)
            {
                slots.Add(slot);
            }
        }

        if (slots.Count == 0)
        {
            Debug.LogError("Слоты инвентаря не найдены!");
        }
    }

    private void Update()
    {
        if (panelType != InventoryPanelType.Player)
            return;
        // Тестовые клавиши для добавления предметов
        if (Input.GetKeyDown(KeyCode.Alpha7) && slots.Count > 0)
        {
            Item testItem = itemDatabase.GetItemById(0, 63); // Предмет с ID 1, стек 3
            if (testItem != null) slots[0].SetItem(testItem);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8) && slots.Count > 0)
        {
            Item testItem = itemDatabase.GetItemById(2, 5); // Предмет с ID 2, стек 1
            if (testItem != null) slots[1].SetItem(testItem);
        }
        if (Input.GetKeyDown(KeyCode.Alpha9) && slots.Count > 0)
        {
            Item testItem = itemDatabase.GetItemById(3, 2); // Предмет с ID 3, стек 2
            if (testItem != null) slots[2].SetItem(testItem);
        }
    }

    public void resetFrameImagesAndInput()
    {
        foreach (var slot in slots)
        {
            slot.resetCursorInSlot();
        }
    }

    public void LoadChestItems(List<ChestItemEntry> chestItems)
    {
        foreach (var entry in chestItems)
        {
            if (entry.slotIndex >= 0 && entry.slotIndex < slots.Count)
            {
                Item item = itemDatabase.GetItemById(entry.itemId, entry.stackSize);
                slots[entry.slotIndex].SetItem(item);
            }
        }

        // Очистка остальных слотов
        for (int i = 0; i < slots.Count; i++)
        {
            if (!chestItems.Exists(e => e.slotIndex == i))
            {
                slots[i].ClearSlot();
            }
        }
    }

    public List<ChestItemEntry> GetCurrentItems()
    {
        List<ChestItemEntry> currentEntries = new List<ChestItemEntry>();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].HasItem())
            {
                Item item = slots[i].GetItem();

                ChestItemEntry entry = new ChestItemEntry
                {
                    slotIndex = i,
                    itemId = item.id,          // ✅ Используем только ID
                    stackSize = item.stackSize // ✅ И текущее количество
                };

                currentEntries.Add(entry);
            }
        }

        return currentEntries;
    }
}