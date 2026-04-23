using UnityEngine;
public class Inventory3: BasePanel, IInventory3
{
    [InjectAttribute1] private IInventoryPanel3 inventoryPanel3 { get; set; }
    [InjectAttribute1] private IInventorySearch3 inventorySearch3 { get; set; }
    [InjectAttribute1] private IPanel lootPanel { get; set; }
    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }
    public override void Open()
    {
        base.Open();
        inventoryPanel3.LoadInventoryState();
        lootPanel?.Open();
    }
    public override void Close()
    {
        base.Close();
        inventorySearch3.StopSearch(); // прекратить поиск
        inventoryPanel3.ShowAll(); // сброс категорий
        inventoryPanel3.ToggleSortNone(); // сброс фильтров
        inventoryPanel3.SaveInventoryState(); // сохранение
        lootPanel?.Close();
    }
}