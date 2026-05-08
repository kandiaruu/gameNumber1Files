//
// Container panel for the inventory system. On open it loads the saved inventory
// state and opens the loot panel. On close it resets filters, saves state, and
// closes the loot panel.
//

using UnityEngine;

public class Inventory3 : BasePanel, IInventory3
{
    [InjectAttribute1] private IInventoryPanel3 inventoryPanel3 { get; set; }
    [InjectAttribute1] private IInventorySearch3 inventorySearch3 { get; set; }
    [InjectAttribute1] private IPanel lootPanel { get; set; }

    // Injects dependencies on awake
    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    // Loads the saved inventory state and opens the loot panel if available
    public override void Open()
    {
        base.Open();
        inventoryPanel3.LoadInventoryState();
        lootPanel?.Open();
    }

    // Stops any active search, resets category and sort filters, saves the inventory state, and closes the loot panel
    public override void Close()
    {
        base.Close();
        inventorySearch3.StopSearch();
        inventoryPanel3.ShowAll();
        inventoryPanel3.ToggleSortNone();
        inventoryPanel3.SaveInventoryState();
        lootPanel?.Close();
    }
}
