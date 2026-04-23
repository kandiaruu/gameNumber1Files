[System.Serializable]
public class ChestItemEntry
{
    public int slotIndex;
    public int itemId;
    public int stackSize;
    public int recipeUsesLeft; // ← индивидуальный счётчик для рецептов
}
