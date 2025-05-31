using System.Collections.Generic;

public interface IInventoryPanel
{
    void resetFrameImagesAndInput();
    void SetupInventory(int slotsCount, int columns);
    public List<InventorySlot> returnSlots();
}
