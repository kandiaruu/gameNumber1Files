using System.Collections.Generic;
using System.Linq;

public static class InventoryPanelExtensions
{
    public static void FillWithRecipeResults(
    this InventoryPanel panel, InventoryPanel playerPanel,
    List<(int recipeId, int resultStackSize)> recipes,
    ItemDatabase itemDatabase,
    List<StashData> stashes, CraftingDatabase craftingDatabase)
{
    // Очищаем все слоты
    foreach (var slot in panel.craftingSlots)
        slot.ClearSlot();

    // Собираем все id предметов у игрока (инвентарь + сташи)
    HashSet<int> ownedItemIds = new HashSet<int>();
    foreach (var slot in playerPanel.slots)
        if (slot.HasItem())
            ownedItemIds.Add(slot.GetItem().id);
    foreach (var stash in stashes)
        foreach (var entry in stash.items)
            ownedItemIds.Add(entry.itemId);

    // Получаем CraftRecipe для всех recipes (по recipeId)
    List<CraftRecipe> allRecipes = craftingDatabase.recipes;

    var available = new List<(int recipeId, int resultStackSize)>();
    var locked = new List<(int recipeId, int resultStackSize)>();

    foreach (var rec in recipes)
    {
        // Получаем сам рецепт по recipeId!
        var craftRecipe = allRecipes.FirstOrDefault(r => r.id == rec.recipeId);
        if (craftRecipe == null)
            continue;

        if (!craftRecipe.requiresRecipe || ownedItemIds.Contains(craftRecipe.recipeItemId))
        {
            available.Add(rec);
        }
        else
        {
            locked.Add(rec);
        }
    }

    // Сначала доступные, потом заблокированные
    int slotIdx = 0;
    foreach (var recipe in available)
    {
        if (slotIdx >= panel.craftingSlots.Count)
            break;
        var craftRecipe = allRecipes.FirstOrDefault(r => r.id == recipe.recipeId);
        if (craftRecipe == null) continue;

        var item = itemDatabase.GetItemById(craftRecipe.resultItemId, recipe.resultStackSize);
        if (item != null)
            panel.craftingSlots[slotIdx].SetItem(item, craftRecipe.id);

        slotIdx++;
    }
    foreach (var recipe in locked)
    {
        if (slotIdx >= panel.craftingSlots.Count)
            break;
        var craftRecipe = allRecipes.FirstOrDefault(r => r.id == recipe.recipeId);
        if (craftRecipe == null) continue;

        var item = itemDatabase.GetItemById(craftRecipe.resultItemId, recipe.resultStackSize);
        if (item != null)
            panel.craftingSlots[slotIdx].SetItem(item, craftRecipe.id);

        slotIdx++;
    }
}
}