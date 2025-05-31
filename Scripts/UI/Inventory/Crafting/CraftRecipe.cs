using System.Collections.Generic;

[System.Serializable]
public class CraftRecipe
{
    public int id;
    public int resultItemId;         // То, что крафтим
    public int resultStackSize;      // Сколько получаем
    public bool requiresRecipe;      // Нужен ли рецепт для крафта
    public bool isInfinite;          // Бесконечный ли крафт
    public int craftLimit;           // Лимит крафта (если не бесконечно)
    public int recipeItemId;         // <-- ID предмета-рецепта (например, "Книга рецепта")
    public List<CraftIngredient> ingredients = new List<CraftIngredient>();
    public bool isUnlocked = false; // Можно сделать private set и методы управлени
    public CraftRecipe(
        int resultItemId,
        int resultStackSize,
        bool requiresRecipe,
        bool isInfinite,
        int craftLimit,
        int recipeItemId,
        List<CraftIngredient> ingredients)
    {
        this.resultItemId = resultItemId;
        this.resultStackSize = resultStackSize;
        this.requiresRecipe = requiresRecipe;
        this.isInfinite = isInfinite;
        this.craftLimit = craftLimit;
        this.recipeItemId = recipeItemId;
        this.ingredients = ingredients;
    }
}

[System.Serializable]
public class CraftIngredient
{
    public int itemId;
    public int stackSize;

    public CraftIngredient(int itemId, int stackSize)
    {
        this.itemId = itemId;
        this.stackSize = stackSize;
    }
}