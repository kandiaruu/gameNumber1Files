public class Inventory2: BasePanel, IInventory2
{
    [InjectAttribute1] private IInventoryPanel2 inventoryPanel2 { get; set; } // Инъекция зависимости для IInventorySlot

    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }
    public override void Open()
    {
        base.Open();
        inventoryPanel2.SaveInventoryState();
        inventoryPanel2.SetupInventory(24, 8);
        inventoryPanel2.LoadInventoryState();
    }
    public override void Close()
    {
        base.Close(); // Вызываем базовый метод, чтобы панель стала активной
        inventoryPanel2.ReturnHeldItemToOriginalPosition();
    }
}
