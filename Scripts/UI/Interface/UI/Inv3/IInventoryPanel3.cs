using System.Collections.Generic;
public interface IInventoryPanel3
{
    void SetupInventory(int slotCount, int columns);
    void SaveInventoryState();
    void LoadInventoryState();
    void ShowAll();
    void ToggleSortNone();
    void SetSearchQuery(string query);
    void AddItemsFromList(List<InvItemDatabase3> items);
}
