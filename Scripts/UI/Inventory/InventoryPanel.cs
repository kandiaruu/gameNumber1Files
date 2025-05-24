using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
public enum InventoryPanelType
{
    Player,
    Chest,
    Stash
}
public class InventoryPanel : MonoBehaviour, IInventoryPanel, IChestPanel, IStashPanel
{
    [SerializeField] private InventoryPanelType panelType;
    public InventoryPanelType PanelType => panelType; // Геттер, если нужен доступ снаружи
    [SerializeField] public List<InventorySlot> slots = new List<InventorySlot>();
    [SerializeField] private ItemDatabase itemDatabase; // Ссылка на базу данных предметов
    [SerializeField] private GameObject slotPrefab; // Префаб InventorySlot
    [SerializeField] private Transform slotsParent; // GridLayoutGroup (куда добавлять слоты)
    [SerializeField] private GridLayoutGroup grid;
    [InjectAttribute1] private IChestUIController chestcontroller { get; set; } // Инъекция зависимости для IStashPanel
    [InjectAttribute1] private IInventoryPanelsManager inventoryPanelsManager { get; set; } // Инъекция зависимости для IInventoryPanelsManager
    [InjectAttribute1] private IStashManager stashManager { get; set; } // Инъекция зависимости для IItemInfoManager

    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    public void SetSlotCount(int count)
    {
        // Удаляем лишние слоты
        while (slots.Count > count)
        {
            var slot = slots[slots.Count - 1];
            slots.RemoveAt(slots.Count - 1);
            Destroy(slot.gameObject);
        }
        // Добавляем недостающие слоты
        while (slots.Count < count)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsParent);
            slotObj.name = $"Slot {slots.Count + 1}";
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            slot.inventoryPanel = this;
            slots.Add(slot);
        }
    }

    public void SetGridColumns(int columns)
    {
        if (grid == null) grid = slotsParent.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
    }

    // Пример вызова вместе с SetSlotCount
    public void SetupInventory(int slotCount, int columns)
    {
        SetGridColumns(columns);
        SetSlotCount(slotCount);
    }

    private void Update()
    {
        if (panelType == InventoryPanelType.Player)
        {
            if (!InventorySlot.getSearchMode() || InventorySlot.getSearchLocked())
            {
                if (inventoryPanelsManager.OpenPanels.Count == 0)
                {
                    // InventorySlot.typeRealEscape(false);
                    if (Input.GetKeyDown(KeyCode.S))
                    {
                        OpenStashPanel();
                    }
                }
                if (Input.GetKeyDown(KeyCode.F))
                {
                    FilterItemsByStackSize();
                }
                if (Input.GetKeyDown(KeyCode.X))
                {
                    FilterAndDistributeByItemName();
                }
                if (Input.GetKey(KeyCode.D) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    TryMoveMatchingItemsToOtherPanel();
                }
                else if (Input.GetKeyDown(KeyCode.D))
                {
                    TryMoveAllSlotsToOtherPanel();
                }
            }
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
        if (panelType == InventoryPanelType.Chest || panelType == InventoryPanelType.Stash)
        {
            if (!InventorySlot.getSearchMode() || InventorySlot.getSearchLocked())
            {
                if (Input.GetKey(KeyCode.A) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    TryMoveMatchingItemsToOtherPanel();
                }
                else if (Input.GetKeyDown(KeyCode.A))
                {
                    TryMoveAllSlotsToOtherPanel();
                }
            }
        }
        if (panelType == InventoryPanelType.Stash)
        {
            // InventorySlot.typeRealEscape(true);
            if (!InventorySlot.getSearchMode() || InventorySlot.getSearchLocked())
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    SwitchStashNext();
                }
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    SwitchStashPrev();
                }
                if (Input.GetKeyDown(KeyCode.S))
                {
                    OpenStashPanel();
                }
                // Закрытие stash панели (например Escape)
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    chestcontroller.CloseStashUI();
                }
            }
        }
    }

    private void OpenStashPanel()
    {
        var stash = stashManager.GetCurrentStash();
        chestcontroller.OpenStashUI(stash);
    }

    private void SwitchStashNext()
    {
        // Сохраняем текущий stash
        var stash = stashManager.GetCurrentStash();
        chestcontroller.OpenStashUI(stash);

        // Переключаемся на следующий
        stashManager.NextStash();
        stash = stashManager.GetCurrentStash();
        chestcontroller.OpenStashUI(stash);
    }

    private void SwitchStashPrev()
    {
        var stash = stashManager.GetCurrentStash();
        chestcontroller.OpenStashUI(stash);

        stashManager.PrevStash();
        stash = stashManager.GetCurrentStash();
        chestcontroller.OpenStashUI(stash);
    }

    public void TryMoveMatchingItemsToOtherPanel()
    {
        // Получить все другие открытые панели
        var otherPanels = InventoryPanelsManager.Instance.GetPanelsForDoubleClick()
            .Where(p => p != this).ToList();
        if (otherPanels.Count == 0) return;

        // Собираем id всех предметов, которые есть в других панелях
        HashSet<int> otherPanelItemIds = new HashSet<int>();
        foreach (var panel in otherPanels)
        {
            foreach (var slot in panel.slots)
            {
                if (slot.HasItem())
                {
                    otherPanelItemIds.Add(slot.GetItem().id);
                }
            }
        }

        // Перебираем свои слоты и переносим только те предметы, id которых есть в другой панели
        foreach (var slot in slots)
        {
            if (slot.HasItem() && otherPanelItemIds.Contains(slot.GetItem().id))
            {
                slot.TryMoveToOtherPanel();
            }
        }
    }

    private void TryMoveAllSlotsToOtherPanel()
    {
        if (InventorySlot.getSearchLocked())
        {
            foreach (var slot in slots)
            {
                if (slot.isYellow) // Только найденные (выделенные поиском) слоты
                    slot.TryMoveToOtherPanel();
            }
        }
        else
        {
            foreach (var slot in slots)
            {
                slot.TryMoveToOtherPanel();
            }
        }
    }

    public void FilterItemsByStackSize()
    {
        // 1. Собираем данные о предметах из слотов
        var itemData = new List<(int id, int stackSize, Item template)>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].HasItem())
            {
                var item = slots[i].GetItem();
                itemData.Add((
                    item.id,
                    item.stackSize,
                    new Item(item.id, item.itemName, item.description, item.icon, 0, item.maxStackSize, item.isModifiable)
                ));
            }
        }

        // 2. Группируем предметы по id и считаем общее количество
        var itemGroups = new Dictionary<int, (int totalStack, Item template)>();
        foreach (var data in itemData)
        {
            if (itemGroups.ContainsKey(data.id))
            {
                itemGroups[data.id] = (
                    itemGroups[data.id].totalStack + data.stackSize,
                    itemGroups[data.id].template
                );
            }
            else
            {
                itemGroups[data.id] = (
                    data.stackSize,
                    data.template
                );
            }
        }

        // 3. Очищаем все слоты
        foreach (var slot in slots)
        {
            slot.ClearSlot();
        }

        // 4. Перераспределяем предметы по слотам
        // Сортируем по убыванию totalStack, чтобы предметы с большим количеством шли первыми
        int currentSlotIndex = 0;
        foreach (var group in itemGroups.OrderByDescending(g => g.Value.totalStack).ThenBy(g => g.Key))
        {
            int remainingItems = group.Value.totalStack;
            var template = group.Value.template;

            while (remainingItems > 0 && currentSlotIndex < slots.Count)
            {
                int amountToPlace = Mathf.Min(remainingItems, template.maxStackSize);
                if (amountToPlace > 0)
                {
                    var newItem = new Item(
                        template.id,
                        template.itemName,
                        template.description,
                        template.icon,
                        amountToPlace,
                        template.maxStackSize,
                        template.isModifiable
                    );
                    slots[currentSlotIndex].SetItem(newItem);
                    remainingItems -= amountToPlace;
                    currentSlotIndex++;
                }
            }
        }
    }

    public void FilterAndDistributeByItemName()
    {
        // 1. Собираем все предметы по названию (itemName → List<InventorySlot>)
        var slotsByName = new Dictionary<string, List<InventorySlot>>();
        var totalByName = new Dictionary<string, int>();
        var maxStackByName = new Dictionary<string, int>();
        var templateByName = new Dictionary<string, Item>();

        foreach (var slot in slots)
        {
            if (slot.HasItem())
            {
                var itm = slot.GetItem();
                if (!slotsByName.ContainsKey(itm.itemName))
                {
                    slotsByName[itm.itemName] = new List<InventorySlot>();
                    totalByName[itm.itemName] = 0;
                    maxStackByName[itm.itemName] = itm.maxStackSize;
                    templateByName[itm.itemName] = itm;
                }
                slotsByName[itm.itemName].Add(slot);
                totalByName[itm.itemName] += itm.stackSize;
            }
        }

        // 2. Очищаем все слоты
        foreach (var slot in slots)
        {
            if (slot.HasItem())
                slot.ClearSlot();
        }

        // 3. Сортируем названия по алфавиту (A→Z)
        var sortedNames = slotsByName.Keys.OrderBy(name => name, System.StringComparer.Ordinal).ToList();

        // 4. Перебираем имена, распределяем по слотам в порядке алфавита
        int slotIdx = 0;
        foreach (var name in sortedNames)
        {
            int total = totalByName[name];
            int maxStack = maxStackByName[name];
            Item template = templateByName[name];

            while (total > 0 && slotIdx < slots.Count)
            {
                int toPlace = Mathf.Min(total, maxStack);
                var newItem = new Item(template.id, template.itemName, template.description, template.icon, toPlace, template.maxStackSize, template.isModifiable);
                slots[slotIdx].SetItem(newItem);
                total -= toPlace;
                slotIdx++;
            }
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