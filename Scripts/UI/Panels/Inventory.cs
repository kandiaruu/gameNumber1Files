public class Inventory : BasePanel, IInventory
{
    [InjectAttribute1] private IInventoryPanel inventoryPanel { get; set; } // Инъекция зависимости для IInventorySlot
    [InjectAttribute1] private IChestPanel chestPanel { get; set; } // Инъекция зависимости для IInventorySlot
    [InjectAttribute1] private ITooltipManager tooltipManager { get; set; } // Инъекция зависимости для IInventorySlot
    [InjectAttribute1] private IChestUIController chestUIController { get; set; } // Контроллер UI сундука
    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }
    public override void Open()
    {
        base.Open();
        inventoryPanel.SetupInventory(24, 8);
    }
    public override void Close()
    {
        base.Close(); // Вызываем базовый метод, чтобы панель стала активной
        chestUIController.CloseChestUI(); // Закрываем UI сундука, если он открыт
        chestUIController.CloseStashUI();
        InventorySlot.resetSearchMod(); // Сбрасываем режим поиска
        InventorySlot.ReturnHeldItem(); // Возвращаем удерживаемый предмет в инвентарь
        tooltipManager.HideTooltip(); // Скрываем тултип, если он открыт
        inventoryPanel.resetFrameImagesAndInput(); // Сбрасываем изображения рамок и ввод
        chestPanel.resetFrameImagesAndInput(); // Сбрасываем изображения рамок и ввод для сундука
        chestUIController.SetUIPositionCenter(); // Устанавливаем позицию UI сундука в центр
    }
}
