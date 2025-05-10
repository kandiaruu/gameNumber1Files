public class Inventory : BasePanel, IInventory
{
    [InjectAttribute1] private IInventoryPanel inventoryPanel { get; set; } // Инъекция зависимости для IInventorySlot
    [InjectAttribute1] private ITooltipManager tooltipManager { get; set; } // Инъекция зависимости для IInventorySlot
    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }
    public override void Close()
    {
        base.Close(); // Вызываем базовый метод, чтобы панель стала активной
        InventorySlot.ReturnHeldItem(); // Возвращаем удерживаемый предмет в инвентарь
        tooltipManager.HideTooltip(); // Скрываем тултип, если он открыт
        inventoryPanel.resetFrameImagesAndInput(); // Сбрасываем изображения рамок слотов инвентаря
    }
}
