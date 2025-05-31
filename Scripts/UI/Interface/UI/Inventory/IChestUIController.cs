public interface IChestUIController
{
    void CloseChestUI();
    void OpenChestUI(Chest chest);
    void SetUIPositionCenter();
    void OpenStashUI(StashData stash);
    void CloseStashUI();
    void OpenCraftingUI();
    void CloseCraftingUI();
    void UpdateCraftingUI();
    InventoryPanel returnChestPanel();
}
