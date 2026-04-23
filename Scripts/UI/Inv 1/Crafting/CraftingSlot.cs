using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Interactions;

public class CraftingSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image progressBorderImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image realbackgroundImage;
    [SerializeField] private TMP_Text stackText;
    public InventoryPanel inventoryPanel;
    private Item item;
    private Canvas canvas;
    private static CraftingSlot hoveredSlot;
    [InjectAttribute1] private IItemInfoManager itemInfoManager { get; set; }
    [InjectAttribute1] private ITooltipManager tooltipManager { get; set; } // Добавляем инъекцию TooltipManager
    [InjectAttribute1] private IChestUIController chestUIController { get; set; } // Инъекция зависимости для IStashManager
    private bool isCursorInSlot;
    private Color initialBorderColor = Color.white;
    private Color progressBorderColor = Color.yellow;
    private Color modifiableBorderColor = new Color(0.83f, 0.83f, 0.83f);  // Light Gray (#D3D3D3)
    private Color greenBackgroundColor = new Color(94f / 255f, 156f / 255f, 110f / 255f, 150f/255f);
    private Color redBackgroundColor = new Color(184f / 255f, 80f / 255f, 80f / 255f, 150f/255f);
    private Color blueBackgroundColor = new Color(86f / 255f, 142f / 255f, 198f / 255f, 83f/255f);
    private Color yellowBackgroundColor = new Color(255f/255f, 192f/255f, 0f/255f, 150f/255f); // Прозрачный белый цвет
    [InjectAttribute1] private IStashManager stashManager { get; set; } // Инъекция зависимости для IItemInfoManager
    [InjectAttribute1] private IInventoryPanel playerInventoryPanel { get; set; } // Инъекция зависимости для IInventoryPanelsManager
    public CraftingDatabase craftingDatabase; // назначается через инспектор или DI
    public ItemDatabase itemDatabase; // назначается через инспектор или DI
    public bool isYellow;
    public static string searchInput = "";
    private static bool searchActive = false;
    private static bool isSearchMode = false;
    private static bool isSearchLocked = false; // Блокировка ввода после Enter
    private int recipeId = -1;
    
    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        if (itemImage == null || backgroundImage == null || progressBorderImage == null || stackText == null)
        {
            Debug.LogError("Один из компонентов не назначен для слота инвентаря!");
        }

        backgroundImage.enabled = true;
        itemImage.enabled = false;
        progressBorderImage.enabled = false;
        stackText.enabled = false;
        realbackgroundImage.enabled = false;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas не найден!");
        }

        // backgroundImage.color = initialBorderColor;
        backgroundImage.color = modifiableBorderColor;
        progressBorderImage.color = progressBorderColor;

        progressBorderImage.type = Image.Type.Filled;
        progressBorderImage.fillMethod = Image.FillMethod.Radial360;
        progressBorderImage.fillOrigin = (int)Image.Origin360.Top;
        progressBorderImage.fillClockwise = true;
        progressBorderImage.fillAmount = 0f;

        DependencyContainer1.InjectDependencies(this);
    }

    private void Start()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("InventoryPanel не назначен для слота!");
        }
    }

    private void Update()
    {

        if (item != null && isCursorInSlot)
        {
            frameImage.enabled = true;
        }
        else
        {
            frameImage.enabled = false;
        }

        if (item != null)
        {
            if (isYellow)
            {
                realbackgroundImage.color = yellowBackgroundColor;
            }
            else
            {
                CraftRecipe recipe = craftingDatabase.GetRecipeById(recipeId);
                var inventorySlots = playerInventoryPanel.returnSlots();
                var allStashes = stashManager.GetAllStashes();
                realbackgroundImage.enabled = true;
                if (HasEnoughIngredients(recipe, inventorySlots, allStashes) && HasRecipeItem(recipe, inventorySlots, allStashes))
                {
                    realbackgroundImage.color = greenBackgroundColor;
                }
                else if (!HasRecipeItem(recipe, inventorySlots, allStashes))
                {
                    realbackgroundImage.color = redBackgroundColor;
                }
                else
                {
                    realbackgroundImage.color = blueBackgroundColor;
                }
            }
        }
        else
        {
            realbackgroundImage.enabled = false;
        }
    }

    public static void HandleSlotNameSearch(List<CraftingSlot> allSlots)
    {
        // Подсветка слотов по совпадению с поиском
        foreach (var slot in allSlots)
        {
            if (searchActive && searchInput.Length > 0
                && slot.item != null
                && slot.item.itemName.IndexOf(searchInput, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                slot.isYellow = true;
            }
            else
            {
                slot.isYellow = false;
            }
        }

        // Shift + Enter фиксирует ввод
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            if (isSearchMode)
            {
                searchInput = "";
                isSearchLocked = false;
                searchActive = false;
            }
            return;
        }

        // Enter без Shift
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!isSearchMode)
            {
                isSearchMode = true;
                return;
            }
            else if (!isSearchLocked)
            {
                isSearchLocked = true;
                return;
            }
            else
            {
                isSearchLocked = false; // Разблокировка для продолжения ввода
                return;
            }
        }

        // Если не в режиме поиска — выход
        if (!isSearchMode) return;

        // Escape — сброс всего
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            searchInput = "";
            searchActive = false;
            isSearchMode = false;
            isSearchLocked = false;

            foreach (var slot in allSlots)
                slot.realbackgroundImage.enabled = false;

            return;
        }

        // Если ввод зафиксирован — блокируем изменения
        if (isSearchLocked) return;

        // Backspace
        if (Input.GetKeyDown(KeyCode.Backspace) && searchInput.Length > 0)
        {
            searchInput = searchInput.Substring(0, searchInput.Length - 1);
            searchActive = searchInput.Length > 0;
        }

        // Ввод латинских букв
        for (KeyCode k = KeyCode.A; k <= KeyCode.Z; k++)
        {
            if (Input.GetKeyDown(k))
            {
                char c = k.ToString()[0];
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                    c = Char.ToLower(c);

                searchInput += c;
                searchActive = true;
                break;
            }
        }
    }

    public static void resetSearchMod()
    {
        isSearchMode = false;
        searchInput = "";
        searchActive = false;
    }
    
    public bool HasItem()
    {
        return item != null;
    }

    public Item GetItem()
    {
        return item;
    }

    public void SetItem(Item newItem,  int id)
    {
        item = newItem;
        recipeId = id;

        if (isCursorInSlot) ShowItemTooltip();

        if (item != null)
        {
            itemImage.sprite = item.icon;
            itemImage.enabled = true;
            stackText.text = item.stackSize > 1 ? item.stackSize.ToString() : "";
            stackText.enabled = item.stackSize > 1;
            changeItemColor(item); // Изменяем цвет и прозрачность изображения в зависимости от наличия рецепта
        }
        else
        {
            ClearSlot();
        }
    }

    public void changeItemColor(Item newItem)
    {
        // item = newItem;
        // if (item != null)
        // {
        //     CraftRecipe recipe = craftingDatabase.GetRecipeById(recipeId);
        //     var inventorySlots = playerInventoryPanel.returnSlots();
        //     var allStashes = stashManager.GetAllStashes();
        //     itemImage.color =  HasRecipeItem(recipe, inventorySlots, allStashes) ? Color.white : new Color(1, 1, 1, 0.5f); // <--- вот тут
        // }
    }

    public void ClearSlot()
    {
        recipeId = -1;
        item = null;
        itemImage.sprite = null;
        itemImage.enabled = false;
        stackText.text = "";
        stackText.enabled = false;
        itemInfoManager?.HideItemInfo();
        progressBorderImage.enabled = false;
        progressBorderImage.fillAmount = 0f;
        tooltipManager?.HideTooltip(); // Скрываем тултип при очистке слота
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        CraftRecipe recipe = craftingDatabase.GetRecipeById(recipeId);
        var inventorySlots = playerInventoryPanel.returnSlots();
        var allStashes = stashManager.GetAllStashes();

        if (!HasRecipeItem(recipe, inventorySlots, allStashes))
        {
            Debug.Log("У вас нет предмета-рецепта! Крафт невозможен.");
            return;
        }

        if (!HasEnoughIngredients(recipe, inventorySlots, allStashes))
        {
            Debug.Log("Недостаточно ингредиентов для крафта!");
            return;
        }

        int resultId = recipe.resultItemId;
        int resultStack = recipe.resultStackSize;
        int maxStack = item.maxStackSize;
        if (!CanPlaceResultItem(resultId, resultStack, inventorySlots, allStashes, maxStack))
        {
            Debug.Log("Нет места для результата!");
            return;
        }

        // Вычитаем ингредиенты
        foreach (var ing in recipe.ingredients)
            RemoveItemsFromSources(ing.itemId, ing.stackSize, inventorySlots, allStashes);

        // --- ↓↓↓ ДОБАВЬТЕ ЭТО ↓↓↓ ---
        // Если рецепт требует предмет-рецепт и он не бесконечный
        if (recipe.requiresRecipe && !recipe.isInfinite)
        {
            int recipeItemId = recipe.recipeItemId;
            // 1. Поиск предмета-рецепта в инвентаре
            bool foundAndUsedRecipe = false;
            foreach (var slot in inventorySlots)
            {
                if (slot.HasItem())
                {
                    Item it = slot.GetItem();
                    if (it.id == recipeItemId && it.isRecipe)
                    {
                        if (it.recipeUsesLeft != -1 && it.recipeUsesLeft > 0)
                        {
                            it.recipeUsesLeft--;
                            if (it.recipeUsesLeft <= 0)
                            {
                                slot.ClearSlot();
                            }
                            else
                            {
                                slot.SetItem(it);
                            }
                            foundAndUsedRecipe = true;
                            Debug.Log($"Found recipe item: {it.itemName} (uses left: {it.recipeUsesLeft})");
                            break;
                        }
                    }
                }
            }
            // 2. Если не нашли — ищем в stash
            if (!foundAndUsedRecipe)
            {
                foreach (var stash in allStashes)
                {
                    foreach (var entry in stash.items)
                    {
                        if (entry.itemId == recipeItemId)
                        {
                            Item baseRecipeItem = itemDatabase.GetItemById(entry.itemId, entry.stackSize);
                            if (baseRecipeItem.isRecipe && entry.recipeUsesLeft != -1 && entry.recipeUsesLeft > 0)
                            {
                                entry.recipeUsesLeft--;
                                if (entry.recipeUsesLeft <= 0)
                                {
                                    stash.items.Remove(entry);
                                }
                                goto recipeFound;
                            }
                        }               
                    }
                }
            }
        recipeFound:;
        }

        changeItemColor(item); // Обновляем цвет и прозрачность изображения
        // --- ↑↑↑ ДОБАВЬТЕ ЭТО ↑↑↑ ---

        // Кладём результат
        Item resultItem = itemDatabase.GetItemById(resultId, resultStack);
        if (TryAddToSources(resultItem, inventorySlots, allStashes, maxStack))
        {
            Debug.Log("Успешно добавлено в инвентарь или сташ!");
            chestUIController.UpdateCraftingUI();
        }
        else
            Debug.Log("Не удалось добавить результат в инвентарь или сташ!");
    }

    public bool HasRecipeItem(CraftRecipe recipe, List<InventorySlot> inventory, List<StashData> stashes)
    {
        if (!recipe.requiresRecipe) return true; // Если не нужен предмет-рецепт — всегда true

        // Ищем предмет-рецепт в инвентаре
        foreach (var slot in inventory)
            if (slot.HasItem() && slot.GetItem().id == recipe.recipeItemId)
                return true;

        // Ищем предмет-рецепт в сташах
        foreach (var stash in stashes)
            if (stash.items.Any(e => e.itemId == recipe.recipeItemId))
                return true;

        return false;
    }

    private void RemoveItemsFromSources(int itemId, int count, List<InventorySlot> inventory, List<StashData> stashes)
    {
        count = RemoveFromInventory(itemId, count, inventory);
        if (count > 0)
            RemoveFromStashes(itemId, count, stashes);
    }

    private int RemoveFromInventory(int itemId, int count, List<InventorySlot> inventory)
    {
        foreach (var slot in inventory)
        {
            if (count <= 0) break;
            if (slot.HasItem() && slot.GetItem().id == itemId)
            {
                var itm = slot.GetItem();
                int take = Math.Min(itm.stackSize, count);
                itm.stackSize -= take;
                if (itm.stackSize <= 0) slot.ClearSlot();
                else slot.SetItem(itm);
                count -= take;
            }
        }
        return count;
    }

    private void RemoveFromStashes(int itemId, int count, List<StashData> stashes)
    {
        foreach (var stash in stashes)
        {
            foreach (var entry in stash.items)
            {
                if (count <= 0) break;
                if (entry.itemId == itemId && entry.stackSize > 0)
                {
                    int take = Math.Min(entry.stackSize, count);
                    entry.stackSize -= take;
                    count -= take;
                }
            }
            stash.items.RemoveAll(e => e.stackSize <= 0);
            if (count <= 0) break;
        }
    }

    private bool TryAddToSources(Item item, List<InventorySlot> inventory, List<StashData> stashes, int maxStackSize)
    {
        int toPlace = item.stackSize;

        // 1. Сначала - добавить в существующие стаки инвентаря
        foreach (var slot in inventory)
        {
            if (slot.HasItem())
            {
                var itm = slot.GetItem();
                if (itm.id == item.id && itm.stackSize < itm.maxStackSize)
                {
                    int take = Math.Min(itm.maxStackSize - itm.stackSize, toPlace);
                    itm.stackSize += take;
                    slot.SetItem(itm);
                    toPlace -= take;
                    if (toPlace <= 0) return true;
                }
            }
        }
        // 2. Потом - в пустые слоты инвентаря
        foreach (var slot in inventory)
        {
            if (!slot.HasItem())
            {
                int put = Math.Min(toPlace, maxStackSize);
                slot.SetItem(itemDatabase.GetItemById(item.id, put));
                toPlace -= put;
                if (toPlace <= 0) return true;
            }
        }
        // 3. Потом - в стаки стэшей
        foreach (var stash in stashes)
        {
            foreach (var entry in stash.items)
            {
                if (entry.itemId == item.id && entry.stackSize < maxStackSize)
                {
                    int take = Math.Min(maxStackSize - entry.stackSize, toPlace);
                    entry.stackSize += take;
                    toPlace -= take;
                    if (toPlace <= 0) return true;
                }
            }
            // В пустые слоты стэша
            int freeSlots = stash.stashSlots - stash.items.Count;
            while (toPlace > 0 && freeSlots > 0)
            {
                int put = Math.Min(toPlace, maxStackSize);
                stash.items.Add(new ChestItemEntry { itemId = item.id, stackSize = put });
                toPlace -= put;
                freeSlots--;
                if (toPlace <= 0) return true;
            }
        }
        return toPlace <= 0;
    }

    // ============= Рецепт ==================
    public static bool HasEnoughIngredients(CraftRecipe recipe, List<InventorySlot> inventory, List<StashData> stashes)
    {
        foreach (var ing in recipe.ingredients)
        {
            int total = inventory.Where(slot => slot.HasItem() && slot.GetItem().id == ing.itemId).Sum(slot => slot.GetItem().stackSize) +
                        stashes.Sum(stash => stash.items.Where(e => e.itemId == ing.itemId).Sum(e => e.stackSize));
            if (total < ing.stackSize)
            {
                return false;
            }
        }
        return true;
    }

    public static bool CanPlaceResultItem(int itemId, int stackSize, List<InventorySlot> inventory, List<StashData> stashes, int maxStackSize)
        => CanPlaceInSlots(itemId, stackSize, inventory, maxStackSize) || stashes.Any(s => CanPlaceInStash(itemId, stackSize, s, maxStackSize));

    private static bool CanPlaceInSlots(int itemId, int stackSize, List<InventorySlot> slots, int maxStackSize)
    {
        int needed = stackSize;

        foreach (var slot in slots)
        {
            if (slot.HasItem())
            {
                var itm = slot.GetItem();
                if (itm.id == itemId && itm.stackSize < itm.maxStackSize)
                {
                    int toAdd = Math.Min(itm.maxStackSize - itm.stackSize, needed);
                    needed -= toAdd;
                    if (needed <= 0) return true;
                }
            }
        }
        int emptySlots = slots.Count(s => !s.HasItem());
        int slotsNeeded = (int)Math.Ceiling((double)needed / maxStackSize);
        return emptySlots >= slotsNeeded;
    }

    private static bool CanPlaceInStash(int itemId, int stackSize, StashData stash, int maxStackSize)
    {
        int needed = stackSize;
        foreach (var entry in stash.items)
        {
            if (entry.itemId == itemId && entry.stackSize < maxStackSize)
            {
                int toAdd = Math.Min(maxStackSize - entry.stackSize, needed);
                needed -= toAdd;
                if (needed <= 0) return true;
            }
        }
        int emptySlots = stash.stashSlots - stash.items.Count;
        int slotsNeeded = (int)Math.Ceiling((double)needed / maxStackSize);
        return emptySlots >= slotsNeeded;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isCursorInSlot = true;
        hoveredSlot = this;

        if (item != null)
        {
            ShowItemTooltip();
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        isCursorInSlot = false;
        hoveredSlot = null;

        if (tooltipManager != null && tooltipManager.IsOpen)
        {
            tooltipManager.HideTooltip();
        }
    }

    private void ShowItemTooltip()
    {
        if (item != null && tooltipManager != null)
        {
            string content = $"Предмет: {item.itemName}";

            // Добавим ингредиенты крафта, если это крафтовый слот и есть рецепт
            if (craftingDatabase != null)
            {
                CraftRecipe recipe = craftingDatabase.GetRecipeById(recipeId);
                if (recipe != null && recipe.ingredients != null && recipe.ingredients.Count > 0)
                {
                    content += "\nТребуется для крафта:";
                    foreach (var ing in recipe.ingredients)
                    {
                        // Попробуем получить имя ингредиента (если база предметов назначена)
                        string ingName = itemDatabase != null
                            ? itemDatabase.GetItemById(ing.itemId, 1)?.itemName ?? $"ID:{ing.itemId}"
                            : $"ID:{ing.itemId}";
                        content += $"\n - {ingName}: {ing.stackSize}";
                    }
                }
            }

            tooltipManager.ShowTooltip(content, Input.mousePosition);
        }
    }

    public void resetCursorInSlot()
    {
        isCursorInSlot = false;
    }
}