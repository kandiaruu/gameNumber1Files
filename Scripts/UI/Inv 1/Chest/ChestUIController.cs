using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ChestUIController : MonoBehaviour, IChestUIController
{
    [SerializeField] private GameObject chestTitleText;
    [SerializeField] private GameObject chestSkitGrid;
    [SerializeField] private GameObject stashTitleText;
    [SerializeField] private GameObject stashSkitGrid;
    [SerializeField] private GameObject craftingTitleText;
    [SerializeField] private GameObject craftingSkitGrid;
    [SerializeField] private GameObject TitleText;
    [SerializeField] private GameObject SkitGrid;
    private RectTransform titleTextRect;
    private RectTransform skitGridRect;
    [SerializeField] private InventoryPanel playerInventoryPanel; // 💡 Панель инвентаря игрока
    [SerializeField] private InventoryPanel inventoryPanel; // 💡 Панель инвентаря для сундука
    [SerializeField] private InventoryPanel stashPanel; // 💡 Панель инвентаря для хранилища
    [SerializeField] private InventoryPanel craftingPanel; // 💡 Панель инвентаря для хранилища
    [InjectAttribute1] private IStashManager stashManager { get; set; } // Инъекция зависимости для IItemInfoManager
    [InjectAttribute1] private ITooltipManager tooltipManager { get; set; } // Инъекция зависимости для IInventoryPanelsManager
    [SerializeField] private CraftingDatabase craftingDatabase;
    [SerializeField] private ItemDatabase itemDatabase; // 💡 База данных предметов
    private StashData currentStash; // 📦 Активный тайник
    
    private Chest currentChest; // 🗝️ Активный сундук
    private bool isOpenChest = false;
    private bool isOpenStash = false;
    private bool isOpenCrafting = false;
    private void Awake()
    {
        // Получаем RectTransform'ы из GameObject'ов
        titleTextRect = TitleText.GetComponent<RectTransform>();
        skitGridRect = SkitGrid.GetComponent<RectTransform>();
        craftingDatabase.ResetAllRecipeUnlocks();
    }

    public void OpenChestUI(Chest chest)
    {
        if (isOpenChest)
        {
            CloseChestUI(); // если открыт — закроем
            return;
        }

        isOpenChest = true;
        currentChest = chest;

        chestTitleText.SetActive(true);
        chestSkitGrid.SetActive(true);
        SetUIPositionLeft(); // Устанавливаем позицию UI влево

        int slotsCount = chest.GetSlotsCount();
        int columns = chest.size == ChestSize.Small ? 4 : 6;
        inventoryPanel.SetupInventory(slotsCount, columns);
        
        // 📦 Загружаем предметы сундука в UI
        inventoryPanel.LoadChestItems(currentChest.chestItems);
        InventoryPanelsManager.Instance.RegisterOpenPanel(inventoryPanel);
    }

    public void OpenStashUI(StashData stash)
    {
        CloseCraftingUI(); // Закрываем тайник, если открыт
        CloseChestUI(); // Закрываем сундук, если открыт
        if (isOpenStash)
        {
            CloseStashUI(); // если открыт — закроем
            return;
        }
        isOpenStash = true;
        currentStash = stash;
        stashTitleText.SetActive(true);
        stashSkitGrid.SetActive(true);
        UpdateStashTitle(); // Обновляем заголовок тайника
        SetUIPositionLeft(); // Устанавливаем позицию UI влево
        stashPanel.SetupInventory(stash.stashSlots, stash.stashColumns); // Настройка инвентаря с 24 слотами и 8 колонками
        stashPanel.LoadChestItems(stash.items);
        InventoryPanelsManager.Instance.RegisterOpenPanel(stashPanel);
    }

    public void CloseStashUI()
    {
        isOpenStash = false;
        stashTitleText.SetActive(false);
        stashSkitGrid.SetActive(false);
        SetUIPositionCenter();
        if (currentStash != null)
        {
            currentStash.items = stashPanel.GetCurrentItems();
        }
        InventoryPanelsManager.Instance.UnregisterPanel(stashPanel);
        tooltipManager.HideTooltip(); // Скрываем тултип, если он открыт
        stashPanel.resetFrameImagesAndInput(); // Сбрасываем изображения рамок и ввод
    }

    private void UpdateStashTitle()
    {
        if (stashTitleText != null)
        {
            int index = stashManager.GetCurrentStashIndex() + 1; // +1 чтобы был Stash 1, Stash 2...
            var textComponent = stashTitleText.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"Stash {index}";
            }
        }
    }

    public void CloseChestUI()
    {
        isOpenChest = false;

        chestTitleText.SetActive(false);
        chestSkitGrid.SetActive(false);
        if (currentChest != null)
        {
            List<ChestItemEntry> updatedItems = inventoryPanel.GetCurrentItems();
            currentChest.SaveChestItems(updatedItems);
            currentChest = null;
        }
        InventoryPanelsManager.Instance.UnregisterPanel(inventoryPanel);
    }

    public InventoryPanel returnChestPanel() => inventoryPanel;

    public void OpenCraftingUI()
    {
        CloseChestUI();
        CloseStashUI();
        if (isOpenCrafting)
        {
            CloseCraftingUI();
            return;
        }
        isOpenCrafting = true;
        craftingTitleText.SetActive(true);
        craftingSkitGrid.SetActive(true);
        SetUIPositionLeft();

        // Вот здесь вызываем!
        RecipeFinder.UnlockRecipesByInventory(playerInventoryPanel, stashManager.GetAllStashes(), craftingDatabase);

        int count = RecipeFinder.CountUniqueOwnedRecipes(playerInventoryPanel, stashManager.GetAllStashes(), craftingDatabase);
        var recipes = RecipeFinder.GetOwnedRecipes(playerInventoryPanel, stashManager.GetAllStashes(), craftingDatabase);
        // Debug.Log($"Found {count} unique recipes in inventory and stash.");
        // Debug.Log($"Found {recipes.Count} recipes with results in inventory and stash.");
        craftingPanel.SetupInventory(count, 8);
        craftingPanel.FillWithRecipeResults(
    playerInventoryPanel,
    recipes,
    itemDatabase,
    stashManager.GetAllStashes(),
    craftingDatabase
);
        InventoryPanelsManager.Instance.RegisterOpenPanel(craftingPanel);
    }

    public void UpdateCraftingUI()
    {
        // int count = RecipeFinder.CountUniqueOwnedRecipes(playerInventoryPanel, stashManager.GetAllStashes(), craftingDatabase);
        // var recipes = RecipeFinder.GetOwnedRecipes(playerInventoryPanel, stashManager.GetAllStashes(), craftingDatabase);
        // craftingPanel.SetupInventory(count, 8);        // stashPanel.LoadChestItems(stash.items);
        // craftingPanel.FillWithRecipeResults(recipes, itemDatabase, stashManager.GetAllStashes());
    }

    public void CloseCraftingUI()
    {
        isOpenCrafting = false;
        craftingTitleText.SetActive(false);
        craftingSkitGrid.SetActive(false);
        SetUIPositionCenter();
        InventoryPanelsManager.Instance.UnregisterPanel(craftingPanel);
        tooltipManager.HideTooltip(); // Скрываем тултип, если он открыт
        craftingPanel.resetFrameImagesAndInput(); // Сбрасываем изображения рамок и ввод
    }

    public void SetUIPositionCenter()
    {
        if (titleTextRect != null)
            titleTextRect.anchoredPosition = new Vector2(0, titleTextRect.anchoredPosition.y);
        if (skitGridRect != null)
            skitGridRect.anchoredPosition = new Vector2(0, skitGridRect.anchoredPosition.y);
    }

    // 🔹 Метод 2: установить позиции по X в -400
    public void SetUIPositionLeft()
    {
        if (titleTextRect != null)
            titleTextRect.anchoredPosition = new Vector2(-400, titleTextRect.anchoredPosition.y);
        if (skitGridRect != null)
            skitGridRect.anchoredPosition = new Vector2(-400, skitGridRect.anchoredPosition.y);
    }
}
