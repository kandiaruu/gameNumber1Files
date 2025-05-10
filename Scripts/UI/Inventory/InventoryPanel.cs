using UnityEngine;
using System.Collections.Generic;

public class InventoryPanel : MonoBehaviour, IInventoryPanel
{
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

    private void Start()
    {
        foreach (var slot in slots)
        {
            slot.ClearSlot();
        }
    }

    private void Update()
    {
        // Тестовые клавиши для добавления предметов
        if (Input.GetKeyDown(KeyCode.Alpha7) && slots.Count > 0)
        {
            Item testItem = itemDatabase.GetItemById(0, 63); // Предмет с ID 1, стек 3
            if (testItem != null) slots[0].SetItem(testItem);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8) && slots.Count > 0)
        {
            Item testItem = itemDatabase.GetItemById(2,5); // Предмет с ID 2, стек 1
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
}