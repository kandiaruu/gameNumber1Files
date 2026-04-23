public interface IInventoryPanel2
{
    void SetupInventory(int slotCount, int columns);
    void SaveInventoryState();
    void LoadInventoryState();
    void ReturnHeldItemToOriginalPosition();
}
