public interface IInventorySlotBase
{
    void ClearSlot();
    bool HasItem();
    void SetItem(Item item);
    Item GetItem();
    void resetCursorInSlot();
    
    // Можно добавить другие методы, которые нужны для любого слота
}