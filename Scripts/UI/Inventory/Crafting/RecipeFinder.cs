using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public static class RecipeFinder
{
    // Подсчёт уникальных рецептов (по всем stash и инвентарю)
    public static int CountUniqueOwnedRecipes(
        InventoryPanel playerPanel,
        List<StashData> allStashes,
        CraftingDatabase craftingDatabase)
    {
        var ownedRecipeIds = GetOwnedRecipeIds(playerPanel, allStashes, craftingDatabase, false);
        return ownedRecipeIds.Count;
    }

    // Получить (id результата, stackSize) для всех найденных рецептов
    public static List<(int recipeId, int resultStackSize)> GetOwnedRecipes(
    InventoryPanel playerPanel,
    List<StashData> allStashes,
    CraftingDatabase craftingDatabase)
    {
        var ownedRecipeIds = GetOwnedRecipeIds(playerPanel, allStashes, craftingDatabase, true);
        var result = new List<(int recipeId, int resultStackSize)>();
        foreach (var recipe in craftingDatabase.recipes)
        {
            // Только те рецепты, которые разлочены (их id содержится в ownedRecipeIds)
            if (ownedRecipeIds.Contains(recipe.id))
            {
                result.Add((recipe.id, recipe.resultStackSize));
            }
        }
        return result;
    }

    // Собрать id всех найденных предметов-рецептов из инвентаря и всех stash
    private static HashSet<int> GetOwnedRecipeIds(
        InventoryPanel playerPanel,
        List<StashData> allStashes,
        CraftingDatabase craftingDatabase, bool isRecipe)
    {
        HashSet<int> ownedItemIds = new HashSet<int>();
        // Из инвентаря игрока
        foreach (var slot in playerPanel.slots)
        {
            if (slot.HasItem())
                ownedItemIds.Add(slot.GetItem().id);
        }
        // Из всех stash
        foreach (var stash in allStashes)
        {
            foreach (var entry in stash.items)
            {
                ownedItemIds.Add(entry.itemId);
            }
        }
        // Только те id, которые используются как recipeItemId в базе рецептов
        HashSet<int> ownedRecipeIds = new HashSet<int>();
        foreach (var recipe in craftingDatabase.recipes)
        {
            if (ownedItemIds.Contains(recipe.recipeItemId) || recipe.isUnlocked || !recipe.requiresRecipe)
            { // Если

                ownedRecipeIds.Add(recipe.id);
                // if (isRecipe) ownedRecipeIds.Add(recipe.recipeItemId);
                // else
                // {
                //     ownedRecipeIds.Add(recipe.resultItemId);
                // }
            }
        }

        return ownedRecipeIds;
    }

    public static void UnlockRecipesByInventory(
    InventoryPanel playerPanel,
    List<StashData> allStashes,
    CraftingDatabase craftingDatabase)
    {
        HashSet<int> ownedItemIds = new HashSet<int>();
        foreach (var slot in playerPanel.slots)
            if (slot.HasItem())
                ownedItemIds.Add(slot.GetItem().id);

        foreach (var stash in allStashes)
            foreach (var entry in stash.items)
                ownedItemIds.Add(entry.itemId);

        foreach (var recipe in craftingDatabase.recipes)
        {
            if (ownedItemIds.Contains(recipe.recipeItemId))
                recipe.isUnlocked = true; // Разблокируем навсегда
        }
    }
}