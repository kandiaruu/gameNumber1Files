using UnityEngine;
using System.Collections.Generic;

public class ChestUIController : MonoBehaviour, IChestUIController
{
    [SerializeField] private GameObject chestTitleText;
    [SerializeField] private GameObject chestSkitGrid;
    [SerializeField] private GameObject TitleText;
    [SerializeField] private GameObject SkitGrid;
    private RectTransform titleTextRect;
    private RectTransform skitGridRect;
    [SerializeField] private InventoryPanel inventoryPanel; // 💡 Панель инвентаря для сундука
    
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
