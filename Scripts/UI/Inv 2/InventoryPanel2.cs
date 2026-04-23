using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using Unity.VisualScripting;
using System.ComponentModel;
using System;

public class InventoryPanel2 : MonoBehaviour, IInventoryPanel2
{
    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsParent;

    [Header("Inventory Settings")]
    [SerializeField] private int gridColumns = 6;
    [SerializeField] private int gridRows = 4;
    [SerializeField] private float cellWidth = 64f;
    [SerializeField] private float cellHeight = 64f;

    [Header("Database")]
    [SerializeField] private ItemDatabase2 itemDatabase;
    private List<InventoryItemData> savedItems = new List<InventoryItemData>();

    private List<InventorySlot2> slots = new List<InventorySlot2>();
    private GameObject heldIcon;
    private TextMeshProUGUI heldStackText;
    [SerializeField] private Canvas uiCanvas;
    private int heldItemId = -1;
    private int heldItemStackSize = 0;
    private Vector2Int heldItemSize = Vector2Int.one;
    private Sprite heldItemSprite = null;
    private Vector2Int? heldItemOriginalTopLeft = null; // Верхний левый угол предмета
    private Vector2Int heldItemOriginalSize = Vector2Int.one; // Размер предмета
    InventorySlot2 containerScript;
    // --- Double Click Tracking ---
    InventorySlot2 lastClickedSlot = null;
    private int lastClickedItemId = -1;
    private float lastClickRealTime = 0f;
    private const float doubleClickThreshold = 0.4f; // секунда
    private bool isDoubleMulti = false;
    private List<Vector2Int> lastClickedSlotPositions = new List<Vector2Int>();

    private class MultiSlotContainerInfo
    {
        public GameObject containerObj;
        public List<int> slotIndices; // индексы скрытых слотов
        public int itemId;
        public int stackSize;
        public Vector2Int topLeft;
        public Vector2Int size;
    }
    private List<MultiSlotContainerInfo> activeMultiSlotContainers = new List<MultiSlotContainerInfo>();

    private void Start()
    {
        SetupInventory(gridColumns * gridRows, gridColumns);
    }

    private void Update()
    {
        if (heldIcon != null)
            heldIcon.transform.position = Input.mousePosition;

        if (heldIcon != null)
        {
            HighlightSlotsByHeldItem();
        }
        else
        {
            ClearHighlights();
        }

        // Добавление предмета id 0 по клавише 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryAddItemWithStacking(0, 1);
        }

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
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SaveInventoryState();
            PrintSavedItems();
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            int randomStack = UnityEngine.Random.Range(1, 11);
            TryAddItem(2, randomStack);
        }
    }

    private void HighlightSlotsByHeldItem()
    {
        if (heldIcon == null) return;

        ClearHighlights();

        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            slotsParent as RectTransform,
            mousePos,
            uiCanvas.worldCamera,
            out Vector2 localPoint);

        Vector2Int size = heldItemSize;
        int startX = Mathf.FloorToInt(localPoint.x / cellWidth - (size.x - 1) / 2f);
        int startY = Mathf.FloorToInt(-localPoint.y / cellHeight - (size.y - 1) / 2f);
        Vector2Int startPos = new Vector2Int(startX, startY);

        int gridWidth = gridColumns;
        int gridHeight = gridRows;

        List<InventorySlot2> slotsToHighlight = new List<InventorySlot2>();
        bool hasRed = false;
        bool isOutOfBounds = false;
        bool sameIdExists = false;
        // Первый проход — проверяем, есть ли красные слоты и выход за границы
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                Vector2Int pos = startPos + new Vector2Int(x, y);
                if (pos.x < 0 || pos.y < 0 || pos.x >= gridWidth || pos.y >= gridHeight)
                {
                    isOutOfBounds = true;
                    continue;
                }

                InventorySlot2 slot = GetSlotAtPosition(pos);
                if (slot == null) continue;
                int displayedId = slot.itemId;
                bool isMultiSlot = false;

                foreach (var container in activeMultiSlotContainers)
                {
                    if (container.slotIndices.Contains(slots.IndexOf(slot)))
                    {
                        displayedId = container.itemId; // реальный предмет
                        containerScript = container.containerObj.GetComponent<InventorySlot2>();
                        isMultiSlot = true;
                        break;
                    }
                }

                bool sameId = displayedId == heldItemId && slot.isOccupied;
                // если слот активен то ставим флаг
                if (slot.gameObject.activeSelf)
                    slot.highlightSameId = sameId;
                else
                    if (containerScript != null)
                    slot = containerScript;
                slot.highlightSameId = sameId;
                bool occupied = slot.isOccupied || isMultiSlot;
                if (sameId) sameIdExists = true;
                if (occupied) hasRed = true;
                slotsToHighlight.Add(slot);
            }
        }

        // Второй проход — раскрашиваем слоты
        foreach (var slot in slotsToHighlight)
        {
            bool isMultiSlot = false;
            GameObject highlightTarget = slot.gameObject;

            foreach (var container in activeMultiSlotContainers)
            {
                if (container.slotIndices.Contains(slots.IndexOf(slot)))
                {
                    isMultiSlot = true;
                    highlightTarget = container.containerObj;
                    break;
                }
            }

            Transform bgTransform = highlightTarget.transform.Find("background");
            if (bgTransform != null)
            {
                Image bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.enabled = true;
                    bool occupied = slot.isOccupied || isMultiSlot;

                    if (sameIdExists)
                    {
                        if (occupied)
                        {
                            if (slot.highlightSameId)
                            {
                                // Зеленый для мультислота с таким же ID
                                bgImage.color = new Color(0, 1, 0, 0.5f);
                            }
                            else
                            {
                                bgImage.color = new Color(1, 0, 0, 0.5f);
                            }
                        }
                        else
                        {
                            // Желтый для свободных при наличии такого же ID
                            bgImage.color = new Color(1, 1, 0, 0.5f);
                        }
                    }
                    else if (hasRed)
                    {
                        bgImage.color = occupied ? new Color(1, 0, 0, 0.5f) : new Color(1, 1, 0, 0.5f); // Желтый для занятых и свободных при наличии красного
                    }
                    else if (isOutOfBounds)
                    {
                        bgImage.color = occupied ? new Color(1, 0, 0, 0.5f) : new Color(1, 1, 0, 0.5f); // Желтый если выход за границы
                    }
                    else
                    {
                        bgImage.color = occupied ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 0, 0.5f); // Зеленый по умолчанию
                    }
                }
            }
        }
    }

    private void ClearHighlights()
    {
        // Обычные слоты
        foreach (var slot in slots)
        {
            Transform bgTransform = slot.transform.Find("background");
            if (bgTransform != null)
            {
                Image bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                    bgImage.enabled = false;
            }
            slot.highlightSameId = false;
        }

        // Мульти-слоты
        foreach (var container in activeMultiSlotContainers)
        {
            if (container.containerObj == null) continue;

            Transform bgTransform = container.containerObj.transform.Find("background");
            if (bgTransform != null)
            {
                Image bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                    bgImage.enabled = false;
            }
            if (containerScript != null)
                containerScript.highlightSameId = false;
        }
    }

    private void PrintSavedItems()
    {
        for (int i = 0; i < savedItems.Count; i++)
        {
            var data = savedItems[i];
            string occupied = data.occupiedPositions != null
                ? string.Join(", ", data.occupiedPositions)
                : "null";
            Debug.Log($"[{i}] id: {data.itemId}, stack: {data.stackSize}, topLeft: {data.topLeftPosition}, size: {data.size}, occupied: {occupied}");
        }
    }

    public void SaveInventoryState()
    {
        savedItems.Clear();

        // Сохраняем мультислотовые предметы
        foreach (var info in activeMultiSlotContainers)
        {
            var occupied = new List<Vector2Int>();
            foreach (int idx in info.slotIndices)
                occupied.Add(slots[idx].gridPosition);

            savedItems.Add(new InventoryItemData
            {
                itemId = info.itemId,
                stackSize = info.stackSize,
                topLeftPosition = info.topLeft,
                size = info.size,
                occupiedPositions = occupied
            });
        }

        // Сохраняем одиночные предметы (1x1)
        foreach (var slot in slots)
        {
            if (slot.isOccupied && slot.itemId >= 0 && !IsSlotPartOfMultiSlot(slot))
            {
                savedItems.Add(new InventoryItemData
                {
                    itemId = slot.itemId,
                    stackSize = slot.itemStackSize,
                    topLeftPosition = slot.gridPosition,
                    size = new Vector2Int(1, 1),
                    occupiedPositions = new List<Vector2Int> { slot.gridPosition }
                });
            }
        }
    }

    private bool IsSlotPartOfMultiSlot(InventorySlot2 slot)
    {
        // Проверяет, скрыт ли слот под мультислотом
        return !slot.gameObject.activeSelf;
    }

    public void LoadInventoryState()
    {
        ClearAll();
        foreach (var data in savedItems)
        {
            Item2 item = itemDatabase.GetItemById(data.itemId, data.stackSize);
            if (item != null)
            {
                PlaceItemAtPositions(item, data.topLeftPosition, data.size);
            }
        }
    }

    public void SetupInventory(int slotCount, int columns)
    {
        gridColumns = columns;
        gridRows = Mathf.CeilToInt((float)slotCount / columns);

        foreach (var slot in slots)
            if (slot != null)
                Destroy(slot.gameObject);
        slots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsParent);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            int x = i % gridColumns;
            int y = i / gridColumns;
            slotRect.anchoredPosition = new Vector2(x * cellWidth, -y * cellHeight);
            InventorySlot2 slot = slotObj.GetComponent<InventorySlot2>();
            slot.gridPosition = new Vector2Int(x, y);
            slot.SetDatabase(itemDatabase);
            slot.inventoryPanel = this;
            slotObj.name = $"Slot {x},{y}";
            slots.Add(slot);
        }
    }

    private bool TryAddItemWithStacking(int itemId, int amount)
    {
        Item2 baseItem = itemDatabase.GetItemById(itemId, 1);
        if (baseItem == null)
        {
            Debug.LogError($"Не удалось добавить предмет с ID {itemId}: предмет не найден");
            return false;
        }

        int maxStack = baseItem.maxStackSize;
        int remaining = amount;

        // 1. Дозаполняем обычные (1x1) слоты
        foreach (var slot in slots)
        {
            if (remaining <= 0) break;
            if (!slot.isOccupied || slot.itemId != itemId) continue;
            if (!slot.gameObject.activeSelf) continue; // скрытые слоты мультислота

            int canAdd = maxStack - slot.itemStackSize;
            if (canAdd <= 0) continue;

            int toAdd = Mathf.Min(canAdd, remaining);
            slot.SetItem(itemId, slot.itemStackSize + toAdd, itemDatabase);
            remaining -= toAdd;
        }

        // 2. Дозаполняем мультислоты
        foreach (var container in activeMultiSlotContainers)
        {
            if (remaining <= 0) break;
            if (container.itemId != itemId) continue;

            int canAdd = maxStack - container.stackSize;
            if (canAdd <= 0) continue;

            int toAdd = Mathf.Min(canAdd, remaining);
            container.stackSize += toAdd;
            var contSlot = container.containerObj.GetComponent<InventorySlot2>();
            contSlot.SetItem(itemId, container.stackSize, itemDatabase);
            remaining -= toAdd;
        }

        // 3. Если остались — создаём новые слоты
        while (remaining > 0)
        {
            int stackToPlace = Mathf.Min(maxStack, remaining);
            if (!TryAddItem(itemId, stackToPlace))
                return false;

            remaining -= stackToPlace;
        }

        return true;
    }

    private bool TryAddItem(int itemId, int stackSize)
    {
        Item2 item = itemDatabase.GetItemById(itemId, stackSize);
        if (item == null)
        {
            Debug.LogError($"Не удалось добавить предмет с ID {itemId}: предмет не найден");
            return false;
        }
        for (int y = 0; y < gridRows; y++)
            for (int x = 0; x < gridColumns; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                if (CanPlaceItem(item, position))
                {
                    bool placed = PlaceItem(item, position);
                    if (placed)
                    {
                        return true;
                    }
                }
            }
        Debug.LogWarning($"Не удалось найт�� подходящее место для предмета {item.itemName}");
        return false;
    }

    public bool CanPlaceItem(Item2 item, Vector2Int position)
    {
        if (item == null || position.x < 0 || position.y < 0) return false;
        var (width, height) = item.GetDimensions();
        if (position.x + width > gridColumns || position.y + height > gridRows)
            return false;
        for (int y = position.y; y < position.y + height; y++)
            for (int x = position.x; x < position.x + width; x++)
            {
                InventorySlot2 slot = GetSlotAtPosition(new Vector2Int(x, y));
                if (slot == null || slot.isOccupied || !slot.gameObject.activeSelf)
                    return false;
            }
        return true;
    }

    // Для ручного размещения по координатам (например, при загрузке)
    public bool PlaceItemAtPositions(Item2 item, Vector2Int position, Vector2Int size)
    {
        int width = size.x;
        int height = size.y;

        if (width == 1 && height == 1)
        {
            int idx = position.y * gridColumns + position.x;
            var slot = slots[idx];
            slot.isOccupied = true;
            slot.SetItem(item.id, item.stackSize, itemDatabase);
        }
        else
        {
            List<int> slotsToHide = new List<int>();
            for (int y = position.y; y < position.y + height; y++)
                for (int x = position.x; x < position.x + width; x++)
                {
                    int idx = y * gridColumns + x;
                    slotsToHide.Add(idx);
                    slots[idx].isOccupied = true;
                    slots[idx].gameObject.SetActive(false);
                }

            // ✅ всегда используем один префаб
            GameObject container = Instantiate(slotPrefab, slotsParent);
            RectTransform containerRect = container.GetComponent<RectTransform>();

            // ставим pivot в левый верх
            containerRect.pivot = new Vector2(0, 1);

            // задаём размер
            containerRect.sizeDelta = new Vector2(cellWidth * width, cellHeight * height);

            // позиция верхнего левого слота
            containerRect.anchoredPosition = new Vector2(position.x * cellWidth, -position.y * cellHeight);
            containerRect.sizeDelta = new Vector2(cellWidth * width, cellHeight * height);
            containerRect.anchoredPosition = new Vector2(position.x * cellWidth, -position.y * cellHeight);
            // Увеличиваем дочерние элементы (background, item, frame)
            Transform bg = container.transform.Find("background");
            Transform itemImg = container.transform.Find("item");
            Transform frame = container.transform.Find("frame");
            Transform text = container.transform.Find("Text (TMP)");

            Vector2 newSize = new Vector2(cellWidth * width, cellHeight * height);

            void StretchToParent(RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            if (bg != null) StretchToParent(bg.GetComponent<RectTransform>());
            if (itemImg != null) StretchToParent(itemImg.GetComponent<RectTransform>());
            if (frame != null) StretchToParent(frame.GetComponent<RectTransform>());

            if (text != null)
            {
                RectTransform textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(1, 0);
                textRect.anchorMax = new Vector2(1, 0);
                textRect.pivot = new Vector2(1, 0);
                float offsetX = -cellWidth * 0.2f;
                float offsetY = cellHeight * 0.2f;
                textRect.anchoredPosition = new Vector2(offsetX, offsetY);
            }

            // назначаем предмет
            var containerScript = container.GetComponent<InventorySlot2>();
            if (containerScript != null)
            {
                containerScript.SetDatabase(itemDatabase);
                containerScript.SetItem(item.id, item.stackSize, itemDatabase);
                containerScript.inventoryPanel = this;
            }

            container.name = $"ItemContainer {position.x},{position.y} ({width}x{height})";

            activeMultiSlotContainers.Add(new MultiSlotContainerInfo
            {
                containerObj = container,
                slotIndices = slotsToHide,
                itemId = item.id,
                stackSize = item.stackSize,
                topLeft = position,
                size = size
            });
        }
        return true;
    }

    // Для обычного добавления (поиск места)
    public bool PlaceItem(Item2 item, Vector2Int position)
    {
        var (width, height) = item.GetDimensions();
        return PlaceItemAtPositions(item, position, new Vector2Int(width, height));
    }

    private InventorySlot2 GetSlotAtPosition(Vector2Int position)
    {
        int index = position.y * gridColumns + position.x;
        if (index >= 0 && index < slots.Count)
            return slots[index];
        return null;
    }

    public void ClearItem(int itemId, int stackSize)
    {
        for (int i = activeMultiSlotContainers.Count - 1; i >= 0; i--)
        {
            var info = activeMultiSlotContainers[i];
            if (info.itemId == itemId && info.stackSize == stackSize)
            {
                foreach (int idx in info.slotIndices)
                {
                    slots[idx].ClearSlot();
                    slots[idx].gameObject.SetActive(true);
                }
                Destroy(info.containerObj);
                activeMultiSlotContainers.RemoveAt(i);
            }
        }
    }

    public void ClearAll()
    {
        foreach (var info in activeMultiSlotContainers)
        {
            if (info.containerObj)
                Destroy(info.containerObj);
            foreach (int idx in info.slotIndices)
            {
                slots[idx].ClearSlot();
                slots[idx].gameObject.SetActive(true);
            }
        }
        activeMultiSlotContainers.Clear();

        foreach (var slot in slots)
        {
            slot.ClearSlot();
            slot.gameObject.SetActive(true);
        }
    }

    public void ReturnHeldItemToOriginalPosition()
    {
        if (heldIcon == null || heldItemId < 0 || !heldItemOriginalTopLeft.HasValue)
            return;

        Item2 item = itemDatabase.GetItemById(heldItemId, heldItemStackSize);
        if (item == null) return;

        Vector2Int topLeft = heldItemOriginalTopLeft.Value;
        Vector2Int size = heldItemOriginalSize;

        // Проверяем, можно ли поставить предмет на исходное место
        bool canPlace = true;
        for (int y = 0; y < size.y && canPlace; y++)
            for (int x = 0; x < size.x; x++)
            {
                Vector2Int pos = topLeft + new Vector2Int(x, y);
                InventorySlot2 slot = GetSlotAtPosition(pos);
                if (slot == null || slot.isOccupied || !slot.gameObject.activeSelf)
                {
                    canPlace = false;
                    break;
                }
            }

        if (canPlace)
        {
            PlaceItemAtPositions(item, topLeft, size);

            Destroy(heldIcon);
            heldIcon = null;
            heldStackText = null;
            heldItemId = -1;
            heldItemStackSize = 0;
            heldItemSprite = null;
            heldItemSize = Vector2Int.one;
            heldItemOriginalTopLeft = null;
            heldItemOriginalSize = Vector2Int.one;
        }
        else
        {
            Debug.LogWarning("Невозможно вернуть предмет на исходное место — оно занято.");
        }
    }

    // --- Utility methods for slot logic ---
    private bool TryFindMultiSlot(InventorySlot2 clickedSlot, out MultiSlotContainerInfo foundContainer)
    {
        foreach (var c in activeMultiSlotContainers)
        {
            if (!clickedSlot.gameObject.activeSelf && c.slotIndices.Contains(slots.IndexOf(clickedSlot)))
            {
                foundContainer = c;
                return true;
            }
            if (clickedSlot.transform == c.containerObj.transform)
            {
                foundContainer = c;
                return true;
            }
        }
        foundContainer = null;
        return false;
    }

    private void CreateHeldIcon(Sprite sprite, int stackSize, Vector2Int size, TMP_Text stackTextPrefab)
    {
        if (heldIcon != null)
            Destroy(heldIcon);

        heldIcon = new GameObject("HeldIcon");
        heldIcon.transform.SetParent(uiCanvas.transform, false);
        heldIcon.transform.SetAsLastSibling();

        if (stackSize > 1)
        {
            GameObject textObject = Instantiate(stackTextPrefab.gameObject, heldIcon.transform);
            textObject.transform.SetParent(heldIcon.transform, false);
            heldStackText = textObject.GetComponent<TextMeshProUGUI>();
            heldStackText.text = stackSize.ToString();
            heldStackText.enabled = true;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(1, 0);
            textRect.anchorMax = new Vector2(1, 0);
            textRect.pivot = new Vector2(1, 0);
            textRect.anchoredPosition = new Vector2(-12f, 12f);
            textRect.sizeDelta = new Vector2(200f, 200f);
        }

        Image dragImage = heldIcon.AddComponent<Image>();
        dragImage.sprite = sprite;
        dragImage.preserveAspect = true;
        Vector2 iconSize = new Vector2(cellWidth * size.x, cellHeight * size.y);
        dragImage.rectTransform.sizeDelta = iconSize;
        dragImage.color = Color.white;
        dragImage.raycastTarget = false;
    }

    private void ClearHeldItem()
    {
        if (heldIcon != null) Destroy(heldIcon);
        heldIcon = null;
        heldStackText = null;
        heldItemId = -1;
        heldItemStackSize = 0;
        heldItemSprite = null;
        heldItemSize = Vector2Int.one;
        heldItemOriginalTopLeft = null;
        heldItemOriginalSize = Vector2Int.one;
    }

    private bool CanPlaceItemHere(Vector2Int size, Vector2Int startPos)
    {
        for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                Vector2Int pos = startPos + new Vector2Int(x, y);
                InventorySlot2 s = GetSlotAtPosition(pos);
                if (s == null || s.isOccupied || !s.gameObject.activeSelf)
                {
                    return false;
                }
            }
        return true;
    }

    private void UpdateHeldIconStack()
    {
        if (heldStackText != null)
        {
            if (heldItemStackSize > 1)
                heldStackText.text = heldItemStackSize.ToString();
            else
            {
                heldStackText.text = "";
                heldStackText.enabled = false;
            }
        }
    }

    private Vector2Int GetStartPosFromMouse(Vector2Int size)
    {
        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            slotsParent as RectTransform,
            mousePos,
            uiCanvas.worldCamera,
            out Vector2 localPoint);

        int startX = Mathf.FloorToInt(localPoint.x / cellWidth - (size.x - 1) / 2f);
        int startY = Mathf.FloorToInt(-localPoint.y / cellHeight - (size.y - 1) / 2f);
        return new Vector2Int(startX, startY);
    }

    // --- Refactored Left Click ---
    public void OnSlotLeftClick(InventorySlot2 clickedSlot)
    {
        bool isMulti = TryFindMultiSlot(clickedSlot, out MultiSlotContainerInfo foundContainer);

        // --- Double click detection ---
        if (heldIcon == null && clickedSlot.isOccupied)
        {
            isDoubleMulti = isMulti;
            lastClickedSlot = clickedSlot;
            lastClickedItemId = clickedSlot.itemId;
            lastClickRealTime = Time.realtimeSinceStartup;
            if (isMulti && foundContainer != null)
            {
                // мне нужно сохранить позиции всех слотов мультислота
                lastClickedSlotPositions = new List<Vector2Int>(foundContainer.slotIndices.Select(idx => slots[idx].gridPosition));
            }
        }

        // 1. Берём предмет из слота (обычный клик)
        if (heldIcon == null && clickedSlot.isOccupied)
        {
            // --- Данные предмета ---
            heldItemId = clickedSlot.itemId;
            heldItemStackSize = isMulti && foundContainer != null ? foundContainer.stackSize : clickedSlot.itemStackSize;
            heldItemSprite = isMulti && foundContainer != null
                ? foundContainer.containerObj.GetComponent<InventorySlot2>().itemImage.sprite
                : clickedSlot.itemImage.sprite;
            heldItemSize = isMulti && foundContainer != null ? foundContainer.size : Vector2Int.one;
            heldItemOriginalTopLeft = isMulti && foundContainer != null ? foundContainer.topLeft : clickedSlot.gridPosition;
            heldItemOriginalSize = heldItemSize;

            // --- Удаляем предмет из инвентаря ---
            if (isMulti && foundContainer != null)
            {
                foreach (int idx in foundContainer.slotIndices)
                {
                    slots[idx].ClearSlot();
                    slots[idx].gameObject.SetActive(true);
                }
                Destroy(foundContainer.containerObj);
                activeMultiSlotContainers.Remove(foundContainer);
            }
            else
            {
                clickedSlot.ClearSlot();
            }

            // --- Картинка ---
            CreateHeldIcon(heldItemSprite, heldItemStackSize, heldItemSize, clickedSlot.stackText);
        }
        // 2. Пытаемся положить
        else if (heldIcon != null && !clickedSlot.isOccupied && clickedSlot.gameObject.activeSelf)
        {
            if (lastClickedItemId == heldItemId && (lastClickedSlot == clickedSlot || (isDoubleMulti && lastClickedSlotPositions.Contains(clickedSlot.gridPosition))) && (Time.realtimeSinceStartup - lastClickRealTime <= doubleClickThreshold))
            {
                // Double click detected
                OnSlotDoubleLeftClick();
                // Сбросить двойной клик
                lastClickedItemId = -1;
                lastClickedSlot = null;
                lastClickRealTime = 0f;
                isDoubleMulti = false;
                lastClickedSlotPositions.Clear();
                return;
            }

            Item2 item = itemDatabase.GetItemById(heldItemId, heldItemStackSize);
            Vector2Int size = heldItemSize;
            Vector2Int startPos = GetStartPosFromMouse(size);

            if (CanPlaceItemHere(size, startPos))
            {
                PlaceItemAtPositions(item, startPos, size);
                ClearHeldItem();
                // я хочу узнать InventorySlot2 куда мы положили предмет во временную переменную
            }

        }
        // 3. Достак в существующий слот/контейнер
        else if (heldIcon != null && clickedSlot.isOccupied && clickedSlot.itemId == heldItemId)
        {
            Item2 slotItem = itemDatabase.GetItemById(clickedSlot.itemId, clickedSlot.itemStackSize);
            Item2 heldItem = itemDatabase.GetItemById(heldItemId, heldItemStackSize);

            if (slotItem != null && heldItem != null)
            {
                int canAdd = slotItem.maxStackSize - clickedSlot.itemStackSize;
                if (canAdd > 0)
                {
                    int toAdd = Mathf.Min(canAdd, heldItemStackSize);

                    clickedSlot.SetItem(clickedSlot.itemId, clickedSlot.itemStackSize + toAdd, itemDatabase);

                    // синхронизируем стек мультислота
                    if (isMulti && foundContainer != null)
                        foundContainer.stackSize = clickedSlot.itemStackSize;

                    heldItemStackSize -= toAdd;
                    UpdateHeldIconStack();

                    if (heldItemStackSize <= 0)
                        ClearHeldItem();
                }
            }
        }
    }

    // --- Refactored Right Click ---
    public void OnSlotRightClick(InventorySlot2 clickedSlot)
    {
        bool isMulti = TryFindMultiSlot(clickedSlot, out MultiSlotContainerInfo foundContainer);

        // 1. Забрать половину стека (или 1, если stack == 1)
        if (heldIcon == null && clickedSlot.isOccupied)
        {
            int stackInSlot = isMulti && foundContainer != null ? foundContainer.stackSize : clickedSlot.itemStackSize;
            Vector2Int size = isMulti && foundContainer != null ? foundContainer.size : Vector2Int.one;
            Vector2Int topLeft = isMulti && foundContainer != null ? foundContainer.topLeft : clickedSlot.gridPosition;
            Sprite iconSprite = isMulti && foundContainer != null
                ? foundContainer.containerObj.GetComponent<InventorySlot2>().itemImage.sprite
                : clickedSlot.itemImage.sprite;

            if (stackInSlot > 1)
            {
                int take = stackInSlot / 2;
                int leave = stackInSlot - take;

                heldItemId = clickedSlot.itemId;
                heldItemStackSize = take;
                heldItemSprite = iconSprite;
                heldItemSize = size;
                heldItemOriginalTopLeft = topLeft;
                heldItemOriginalSize = size;

                // обновляем слот/контейнер
                if (isMulti && foundContainer != null)
                {
                    foundContainer.stackSize = leave;
                    var contSlot = foundContainer.containerObj.GetComponent<InventorySlot2>();
                    contSlot.SetItem(heldItemId, leave, itemDatabase);
                }
                else
                {
                    clickedSlot.SetItem(heldItemId, leave, itemDatabase);
                }

                CreateHeldIcon(heldItemSprite, heldItemStackSize, heldItemSize, clickedSlot.stackText);
            }
        }
        // 2. Кладём 1 предмет (для мульти — как предмет, для 1x1 — в слот)
        else if (heldIcon != null)
        {
            // 2.1 Клик по пустому активному слоту
            if (!clickedSlot.isOccupied && clickedSlot.gameObject.activeSelf)
            {
                Vector2Int size = heldItemSize;
                Vector2Int startPos = GetStartPosFromMouse(size);

                if (CanPlaceItemHere(size, startPos))
                {
                    Item2 item = itemDatabase.GetItemById(heldItemId, 1);
                    PlaceItemAtPositions(item, startPos, size);

                    heldItemStackSize -= 1;
                    UpdateHeldIconStack();
                    if (heldItemStackSize <= 0)
                        ClearHeldItem();
                }
            }
            // 2.2 Клик по занятому слоту с тем же id
            else if (clickedSlot.isOccupied && clickedSlot.itemId == heldItemId)
            {
                Item2 slotItem = itemDatabase.GetItemById(clickedSlot.itemId, clickedSlot.itemStackSize);
                if (slotItem != null && clickedSlot.itemStackSize < slotItem.maxStackSize)
                {
                    clickedSlot.SetItem(clickedSlot.itemId, clickedSlot.itemStackSize + 1, itemDatabase);

                    if (isMulti && foundContainer != null)
                        foundContainer.stackSize = clickedSlot.itemStackSize;

                    heldItemStackSize -= 1;
                    UpdateHeldIconStack();
                    if (heldItemStackSize <= 0)
                        ClearHeldItem();
                }
            }
        }
    }

public void OnSlotDoubleLeftClick()
{
    // helditem должен быть активен!
    if (heldIcon == null || heldItemId < 0 || heldItemStackSize <= 0)
        return;

    int maxStack = itemDatabase.GetItemById(heldItemId, heldItemStackSize).maxStackSize;
    int heldStackLeft = heldItemStackSize;
    int spaceInHeld = maxStack - heldStackLeft;
    if (spaceInHeld <= 0) return;

    // --- Собираем все подходящие стеки ---
    var candidateStacks = new List<(int stackSize, Action<int> TakeFromStack)>();

    // 1. Обычные (1x1) слоты
    for (int i = 0; i < slots.Count; i++)
    {
        var slot = slots[i];
        // Пропускаем пустые и те, что не совпадают по id
        if (!slot.isOccupied || slot.itemId != heldItemId)
            continue;

        // Если этот слот скрыт — значит он часть мультислота, его не трогаем как 1x1
        if (!slot.gameObject.activeSelf) continue;

        int slotStack = slot.itemStackSize;
        if (slotStack <= 0) continue;

        candidateStacks.Add((slotStack, (toTake) => {
            slot.SetItem(heldItemId, slot.itemStackSize - toTake, itemDatabase);
            if (slot.itemStackSize <= 0) slot.ClearSlot();
        }));
    }

    // 2. Мультислоты (контейнеры)
    foreach (var container in activeMultiSlotContainers.ToList()) // ToList чтобы не было проблем при удалении
    {
        if (container.itemId != heldItemId || container.stackSize <= 0) continue;

        candidateStacks.Add((container.stackSize, (toTake) => {
            container.stackSize -= toTake;
            var contSlot = container.containerObj.GetComponent<InventorySlot2>();
            contSlot.SetItem(heldItemId, container.stackSize, itemDatabase);

            if (container.stackSize <= 0)
            {
                foreach (int idx in container.slotIndices)
                {
                    slots[idx].ClearSlot();
                    slots[idx].gameObject.SetActive(true);
                }
                UnityEngine.Object.Destroy(container.containerObj);
                activeMultiSlotContainers.Remove(container);
            }
        }));
    }

    // --- Сортировка по stacksize (от меньшего к большему) ---
    candidateStacks = candidateStacks.OrderBy(t => t.stackSize).ToList();

    // --- Забираем в helditem до maxStackSize ---
    foreach (var (stackSize, TakeFromStack) in candidateStacks)
    {
        if (spaceInHeld <= 0) break;
        int toTake = Mathf.Min(spaceInHeld, stackSize);

        TakeFromStack(toTake);
        heldStackLeft += toTake;
        spaceInHeld -= toTake;
    }

    heldItemStackSize = heldStackLeft;
    UpdateHeldIconStack();
    // heldIcon НЕ убираем, даже если оно стало полным!
}
}