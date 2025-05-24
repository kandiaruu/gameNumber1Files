using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ChestUIController : MonoBehaviour, IChestUIController
{
    [SerializeField] private GameObject chestTitleText;
    [SerializeField] private GameObject chestSkitGrid;
    [SerializeField] private GameObject stashTitleText;
    [SerializeField] private GameObject stashSkitGrid;
    [SerializeField] private GameObject TitleText;
    [SerializeField] private GameObject SkitGrid;
    private RectTransform titleTextRect;
    private RectTransform skitGridRect;
    [SerializeField] private InventoryPanel inventoryPanel; // 💡 Панель инвентаря для сундука
    [SerializeField] private InventoryPanel stashPanel; // 💡 Панель инвентаря для хранилища
    [InjectAttribute1] private IStashManager stashManager { get; set; } // Инъекция зависимости для IItemInfoManager
    private StashData currentStash; // 📦 Активный тайник
    
    private Chest currentChest; // 🗝️ Активный сундук
    private bool isOpen = false;
    private void Awake()
    {
        // Получаем RectTransform'ы из GameObject'ов
        titleTextRect = TitleText.GetComponent<RectTransform>();
        skitGridRect = SkitGrid.GetComponent<RectTransform>();
    }

    public void OpenChestUI(Chest chest)
    {
        if (isOpen)
        {
            CloseChestUI(); // если открыт — закроем
            return;
        }

        isOpen = true;
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
        if (isOpen)
        {
            CloseStashUI(); // если открыт — закроем
            return;
        }
        isOpen = true;
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
        isOpen = false;
        stashTitleText.SetActive(false);
        stashSkitGrid.SetActive(false);
        SetUIPositionCenter();
        if (currentStash != null)
        {
            currentStash.items = stashPanel.GetCurrentItems();
        }
        InventoryPanelsManager.Instance.UnregisterPanel(stashPanel);
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
        isOpen = false;

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
