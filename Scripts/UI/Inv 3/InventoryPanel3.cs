using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.EventSystems;
using TMPro;

public class InventoryPanel3 : MonoBehaviour, IInventoryPanel3, ILootInventoryPanel3
{
    [Header("Prefabs & Parents & Scroll")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsParent;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject emptyText;
    [Header("Inventory Settings")]
    [SerializeField] private int gridColumns = 6;
    [SerializeField] private int gridRows = 0;
    [SerializeField] private float cellWidth = 64f;
    [SerializeField] private float cellHeight = 64f;

    [Header("Database")]
    [SerializeField] private ItemDatabase3 itemDatabase;
    private List<InvItemDatabase3> savedItems = new List<InvItemDatabase3>();

    private List<InventorySlot3> slots = new List<InventorySlot3>();
    private GameObject heldIcon;
    [SerializeField] private Canvas uiCanvas;
    [Header("Colors")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.5f);
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f);
    [Header("Category Buttons")]
    [SerializeField] private Button allButton;
    [SerializeField] private Button weaponButton;
    [SerializeField] private Button consumableButton;
    private string activeCategory = ""; // "" = все
    [Header("Category Icons")]
    [SerializeField] private Image allIcon;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image consumableIcon;
    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.black;
    [SerializeField] private Color inactiveColor = Color.white;
    [Header("Amount Sort Buttons")]
    [SerializeField] private Button sortAscButton;
    [SerializeField] private Button sortDescButton;
    [SerializeField] private Image sortAscIcon;
    [SerializeField] private Image sortDescIcon;
    [Header("Name Sort Buttons")]
    [SerializeField] private Button sortNameAscButton;   // A-Z
    [SerializeField] private Button sortNameDescButton;  // Z-A
    [SerializeField] private Image sortNameAscIcon;
    [SerializeField] private Image sortNameDescIcon;
    [Header("Date Sort Buttons")]
    [SerializeField] private Button sortDateAscButton;   // старые -> новые
    [SerializeField] private Button sortDateDescButton;  // новые -> старые
    [SerializeField] private Image sortDateAscIcon;
    [SerializeField] private Image sortDateDescIcon;
    [Header("Context Menu")]
    [SerializeField] private GameObject itemContextMenu;
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonLabel;
    [SerializeField] private Button closeButton;
    [SerializeField] private PlayerWeaponController weaponController;
    private InventorySlot3 selectedSlot;
    private enum AmountSortMode { None, Asc, Desc }
    private AmountSortMode amountSortMode = AmountSortMode.None;
    private enum NameSortMode { None, Asc, Desc }
    private NameSortMode nameSortMode = NameSortMode.None;

    private enum DateSortMode { None, Asc, Desc }
    private DateSortMode dateSortMode = DateSortMode.None;
    private string searchQuery = "";
    private void Start()
    {
        SetupInventory(0, gridColumns);

        if (allButton != null)
        {
            allButton.onClick.AddListener(() =>
            {
                ShowAll();
                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        if (weaponButton != null)
        {
            weaponButton.onClick.AddListener(() =>
            {
                FilterByCategory("weapon");
                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        if (consumableButton != null)
        {
            consumableButton.onClick.AddListener(() =>
            {
                FilterByCategory("consumable");
                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        if (sortAscButton != null)
        {
            sortAscButton.onClick.AddListener(() =>
            {
                ToggleSortAsc();
                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        if (sortDescButton != null)
        {
            sortDescButton.onClick.AddListener(() =>
            {
                ToggleSortDesc();
                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        if (sortNameAscButton != null)
        {
            sortNameAscButton.onClick.AddListener(() =>
            {
                ToggleNameAsc();
                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        if (sortNameDescButton != null)
        {
            sortNameDescButton.onClick.AddListener(() =>
            {
                ToggleNameDesc();
                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        if (sortDateAscButton != null)
        {
            sortDateAscButton.onClick.AddListener(() =>
            {
                ToggleDateAsc();
                EventSystem.current.SetSelectedGameObject(null);
            });
        }

        if (sortDateDescButton != null)
        {
            sortDateDescButton.onClick.AddListener(() =>
            {
                ToggleDateDesc();
                EventSystem.current.SetSelectedGameObject(null);
            });
        }
            
        UpdateSortIcons();
    }

    private void Update()
    {
        if (heldIcon != null)
            heldIcon.transform.position = Input.mousePosition;

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            int randomStack = UnityEngine.Random.Range(1, 11);
            TryAddItem(0, randomStack);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ClearAll();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            int randomStack = UnityEngine.Random.Range(1, 11);
            TryAddItem(1, randomStack);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            int randomStack = UnityEngine.Random.Range(1, 11);
            TryAddItem(2, randomStack);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            int randomStack = UnityEngine.Random.Range(1, 11);
            TryAddItem(3, randomStack);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            int randomStack = UnityEngine.Random.Range(1, 11);
            TryAddItem(4, randomStack);
        }
    }

    private void ToggleSortAsc()
    {
        amountSortMode = (amountSortMode == AmountSortMode.Asc) ? AmountSortMode.None : AmountSortMode.Asc;
        UpdateSortIcons();
        RebuildSlots();
    }

    private void ToggleSortDesc()
    {
        amountSortMode = (amountSortMode == AmountSortMode.Desc) ? AmountSortMode.None : AmountSortMode.Desc;

        UpdateSortIcons();
        RebuildSlots();
    }

    public void ToggleSortNone()
    {
        amountSortMode = AmountSortMode.None;
        nameSortMode = NameSortMode.None;
        dateSortMode = DateSortMode.None;
        UpdateSortIcons();
        RebuildSlots();
    }

    private void ToggleNameAsc()
    {
        nameSortMode = (nameSortMode == NameSortMode.Asc) ? NameSortMode.None : NameSortMode.Asc;
        UpdateSortIcons();
        RebuildSlots();
    }

    private void ToggleNameDesc()
    {
        nameSortMode = (nameSortMode == NameSortMode.Desc) ? NameSortMode.None : NameSortMode.Desc;
        UpdateSortIcons();
        RebuildSlots();
    }

    private void ToggleDateAsc()
    {
        dateSortMode = (dateSortMode == DateSortMode.Asc) ? DateSortMode.None : DateSortMode.Asc;
        UpdateSortIcons();
        RebuildSlots();
    }

    private void ToggleDateDesc()
    {
        dateSortMode = (dateSortMode == DateSortMode.Desc) ? DateSortMode.None : DateSortMode.Desc;
        UpdateSortIcons();
        RebuildSlots();
    }

    private void UpdateSortIcons()
    {
        if (sortAscIcon != null)
            sortAscIcon.color = (amountSortMode == AmountSortMode.Asc) ? activeColor : inactiveColor;

        if (sortDescIcon != null)
            sortDescIcon.color = (amountSortMode == AmountSortMode.Desc) ? activeColor : inactiveColor;

        if (sortNameAscIcon != null)
            sortNameAscIcon.color = (nameSortMode == NameSortMode.Asc) ? activeColor : inactiveColor;

        if (sortNameDescIcon != null)
            sortNameDescIcon.color = (nameSortMode == NameSortMode.Desc) ? activeColor : inactiveColor;

        if (sortDateAscIcon != null)
            sortDateAscIcon.color = (dateSortMode == DateSortMode.Asc) ? activeColor : inactiveColor;

        if (sortDateDescIcon != null)
            sortDateDescIcon.color = (dateSortMode == DateSortMode.Desc) ? activeColor : inactiveColor;
    }

    public void ShowAll()
    {
        activeCategory = "";
        RebuildSlots();
        UpdateCategoryIcons();
    }

    public void FilterByCategory(string category)
    {
        if (activeCategory == category)
        {
            ShowAll();
        }
        else
        {
            activeCategory = category.ToLowerInvariant();
            RebuildSlots();
            UpdateCategoryIcons();
        }
    }

    private void UpdateCategoryIcons()
    {
        if (allIcon != null)
            allIcon.color = string.IsNullOrEmpty(activeCategory) ? activeColor : inactiveColor;

        if (weaponIcon != null)
            weaponIcon.color = activeCategory == "weapon" ? activeColor : inactiveColor;
        
        if (consumableIcon != null)
            consumableIcon.color = activeCategory == "consumable" ? activeColor : inactiveColor;
    }

    private string GenerateInstanceId()
    {
        return Guid.NewGuid().ToString("N");
    }

    public void SaveInventoryState()
    {
        savedItems.Clear();

        foreach (var slot in slots)
        {
            if (slot.isOccupied && slot.itemId >= 0)
            {
                savedItems.Add(new InvItemDatabase3
                {
                    instanceId = slot.itemInstanceId,
                    itemId = slot.itemId,
                    stackSize = slot.itemStackSize,
                    firstAddedTicks = slot.firstAddedTicks,
                    lastAddedTicks = slot.lastAddedTicks,
                    isEquipped = slot.itemInstanceId == weaponController.EquippedInstanceId
                });
            }
        }
    }

    public void LoadInventoryState()
    {
        RebuildSlots();
    }

    public void SetupInventory(int slotCount, int columns)
    {
        gridColumns = Mathf.Max(1, columns);
        RebuildSlots();
    }

    public void SetSearchQuery(string query)
    {
        searchQuery = query ?? "";
        RebuildSlots();
    }

    public void AddItemsFromList(List<InvItemDatabase3> items)
    {
        if (items == null) return;

        foreach (var i in items)
            TryAddItem(i.itemId, i.stackSize);
    }
    private void ResetScroll()
    {
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    public bool HasItem(int itemId, int amount = 1)
    {
        int total = 0;
        foreach (var item in savedItems)
        {
            if (item.itemId == itemId)
            {
                total += item.stackSize;
            }
        }
        return total >= amount;
    }

    public void RebuildSlots()
    {
        ResetScroll();
        foreach (var slot in slots)
            if (slot != null)
                Destroy(slot.gameObject);
        slots.Clear();

        // Фильтруем список
        List<InvItemDatabase3> filtered = savedItems;

        if (!string.IsNullOrEmpty(activeCategory))
        {
            filtered = savedItems.Where(data =>
            {
                Item3 item = itemDatabase.GetItemById(data.itemId, data.stackSize);
                if (item == null || item.categories == null) return false;

                return item.categories.Any(c =>
                    !string.IsNullOrEmpty(c) && c.ToLowerInvariant() == activeCategory);
            }).ToList();
        }

        // Сортировка по количеству (stackSize)
        if (amountSortMode != AmountSortMode.None)
        {
            if (amountSortMode == AmountSortMode.Asc)
                filtered = filtered.OrderBy(i => i.stackSize).ToList();
            else
                filtered = filtered.OrderByDescending(i => i.stackSize).ToList();
        }

        // Сортировки (могут работать вместе)
        IOrderedEnumerable<InvItemDatabase3> ordered = null;

        Func<InvItemDatabase3, int> amountKey = i => i.stackSize;
        Func<InvItemDatabase3, string> nameKey = i =>
        {
            Item3 item = itemDatabase.GetItemById(i.itemId, i.stackSize);
            return item != null ? item.itemName ?? "" : "";
        };
        Func<InvItemDatabase3, long> dateKey = i =>
        {
            long last = i.lastAddedTicks;
            long first = i.firstAddedTicks;
            return (last > 0) ? last : first;
        };

        if (amountSortMode != AmountSortMode.None)
        {
            ordered = (amountSortMode == AmountSortMode.Asc)
                ? filtered.OrderBy(amountKey)
                : filtered.OrderByDescending(amountKey);
        }

        if (nameSortMode != NameSortMode.None)
        {
            if (ordered == null)
                ordered = (nameSortMode == NameSortMode.Asc)
                    ? filtered.OrderBy(nameKey)
                    : filtered.OrderByDescending(nameKey);
            else
                ordered = (nameSortMode == NameSortMode.Asc)
                    ? ordered.ThenBy(nameKey)
                    : ordered.ThenByDescending(nameKey);
        }

        if (dateSortMode != DateSortMode.None)
        {
            if (ordered == null)
                ordered = (dateSortMode == DateSortMode.Asc)
                    ? filtered.OrderBy(dateKey)
                    : filtered.OrderByDescending(dateKey);
            else
                ordered = (dateSortMode == DateSortMode.Asc)
                    ? ordered.ThenBy(dateKey)
                    : ordered.ThenByDescending(dateKey);
        }

        if (ordered != null)
            filtered = ordered.ToList();

        // Поиск по названию (игнор пробелов)
        if (!string.IsNullOrEmpty(searchQuery))
        {
            string q = new string(searchQuery.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();

            filtered = filtered.Where(data =>
            {
                Item3 item = itemDatabase.GetItemById(data.itemId, data.stackSize);
                if (item == null || string.IsNullOrEmpty(item.itemName)) return false;

                string name = new string(item.itemName.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
                return name.Contains(q);
            }).ToList();
        }

        int slotCount = filtered.Count;

        if (emptyText != null)
            emptyText.SetActive(slotCount == 0);
            
        if (slotCount == 0)
        {
            gridRows = 0;
            return;
        }

        gridRows = Mathf.CeilToInt((float)slotCount / gridColumns);

        if (contentRect != null)
        {
            float totalHeight = gridRows * cellHeight;
            float totalWidth = gridColumns * cellWidth;
             
            contentRect.sizeDelta = new Vector2(totalWidth, totalHeight);
        }

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsParent);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            int x = i % gridColumns;
            int y = i / gridColumns;
            slotRect.anchoredPosition = new Vector2(x * cellWidth, -y * cellHeight);

            InventorySlot3 slot = slotObj.GetComponent<InventorySlot3>();
            slot.gridPosition = new Vector2Int(x, y);
            slot.SetDatabase(itemDatabase);
            slot.inventoryPanel = this;

            var data = filtered[i];
            slot.SetItem(data.instanceId, data.itemId, data.stackSize, itemDatabase);
            slot.firstAddedTicks = data.firstAddedTicks;
            slot.lastAddedTicks = data.lastAddedTicks;
            slotObj.name = $"Slot {x},{y}";
            slots.Add(slot);

            // NEW: подсветка только экипированного предмета
            if (slot.backgroundImage != null)
            {
                slot.backgroundImage.color = data.isEquipped ? highlightColor : normalColor;
            }
        }

        // if (slot.backgroundImage != null)
        //     {
        //         if (!string.IsNullOrEmpty(searchQuery))
        //         {
        //             string q = new string(searchQuery.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        //             Item3 item = itemDatabase.GetItemById(data.itemId, data.stackSize);
        //             string name = item != null ? item.itemName ?? "" : "";
        //             string cleanName = new string(name.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();

        //             slot.backgroundImage.color = cleanName.Contains(q)
        //                 ? highlightColor // highlight
        //                 : normalColor;  // normal
        //         }
        //         else
        //         {
        //             slot.backgroundImage.color = normalColor;
        //         }
        //     }
    }

public void MarkEquipped(string instanceId)
{
    for (int i = 0; i < savedItems.Count; i++)
        savedItems[i].isEquipped = savedItems[i].instanceId == instanceId;

    RebuildSlots();
}

public void UnmarkEquipped()
{
    for (int i = 0; i < savedItems.Count; i++)
        savedItems[i].isEquipped = false;

    RebuildSlots();
}

private long GetNowTicks()
{
    return DateTime.UtcNow.Ticks;
}

public bool TryAddItem(int itemId, int stackSize)
{
    Item3 item = itemDatabase.GetItemById(itemId, 1);
    if (item == null)
    {
        Debug.LogError($"Не удалось добавить предмет с ID {itemId}: предмет не найден");
        return false;
    }

    long now = GetNowTicks();

    if (item.isStackable)
    {
        var existing = savedItems.FirstOrDefault(i => i.itemId == itemId);
        if (existing != null)
        {
            existing.stackSize += stackSize;

            // даты
            if (existing.firstAddedTicks == 0)
                existing.firstAddedTicks = now;

            existing.lastAddedTicks = now;

            // если старый объект без instanceId — дополним
            if (string.IsNullOrEmpty(existing.instanceId))
                existing.instanceId = GenerateInstanceId();
        }
        else
        {
            savedItems.Add(new InvItemDatabase3
            {
                instanceId = GenerateInstanceId(),
                itemId = itemId,
                stackSize = stackSize,
                firstAddedTicks = now,
                lastAddedTicks = now
            });
        }
    }
    else
    {
        for (int i = 0; i < stackSize; i++)
        {
            savedItems.Add(new InvItemDatabase3
            {
                instanceId = GenerateInstanceId(),
                itemId = itemId,
                stackSize = 1,
                firstAddedTicks = now,
                lastAddedTicks = now
            });
        }
    }

    RebuildSlots();
    return true;
}

public void SetItemsRaw(List<InvItemDatabase3> raw)
{
    savedItems.Clear();

    foreach (var item in raw)
    {
        Item3 def = itemDatabase.GetItemById(item.itemId, 1);
        if (def == null) continue;

        if (def.isStackable)
        {
            savedItems.Add(new InvItemDatabase3
            {
                itemId = item.itemId,
                stackSize = item.stackSize,
                firstAddedTicks = item.firstAddedTicks,
                lastAddedTicks = item.lastAddedTicks
            });
        }
        else
        {
            for (int i = 0; i < item.stackSize; i++)
            {
                savedItems.Add(new InvItemDatabase3
                {
                    itemId = item.itemId,
                    stackSize = 1,
                    firstAddedTicks = item.firstAddedTicks,
                    lastAddedTicks = item.lastAddedTicks
                });
            }
        }
    }

    RebuildSlots();
}

    public void ClearItem(int itemId, int stackSize)
    {
        if (IsStackable(itemId))
        {
            var existing = savedItems.FirstOrDefault(i => i.itemId == itemId);
            if (existing != null)
            {
                existing.stackSize -= stackSize;
                if (existing.stackSize <= 0)
                {
                    savedItems.Remove(existing);
                }
            }
        }
        else
        {
            int removeCount = stackSize;
            for (int i = savedItems.Count - 1; i >= 0 && removeCount > 0; i--)
            {
                if (savedItems[i].itemId == itemId)
                {
                    savedItems.RemoveAt(i);
                    removeCount--;
                }
            }
        }

        RebuildSlots();
    }

    public void ClearAll()
    {
        savedItems.Clear();
        RebuildSlots();
    }

    private bool IsStackable(int itemId)
    {
        Item3 item = itemDatabase.GetItemById(itemId, 1);
        return item != null && item.isStackable;
    }

    public void OnSlotLeftClick(InventorySlot3 slot)
    {
        if (slot == null || !slot.isOccupied) return;

        selectedSlot = slot;

        Item3 item = itemDatabase.GetItemById(slot.itemId, slot.itemStackSize);
        bool isWeapon = item != null && item.categories != null &&
                        item.categories.Any(c => !string.IsNullOrEmpty(c) && c.ToLowerInvariant() == "weapon");

        itemContextMenu.SetActive(true);

        if (!isWeapon)
        {
            equipButton.gameObject.SetActive(false);
            return;
        }
        equipButton.gameObject.SetActive(true);

        bool thisEquipped = weaponController != null && weaponController.IsEquipped(slot.itemInstanceId);
        equipButtonLabel.text = thisEquipped ? "Снять" : "Экипировать";

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() =>
        {
            itemContextMenu.SetActive(false);
        });

        equipButton.onClick.RemoveAllListeners();
        equipButton.onClick.AddListener(() =>
        {
            if (weaponController == null) return;

            if (weaponController.IsEquipped(slot.itemInstanceId))
            {
                weaponController.Unequip();
                UnmarkEquipped();
            }
            else
            {
                weaponController.Equip(slot.itemInstanceId, slot.itemId);
                MarkEquipped(slot.itemInstanceId);
            }

            itemContextMenu.SetActive(false);
        });
    }
}