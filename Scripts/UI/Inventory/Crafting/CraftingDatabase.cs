using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingDatabase", menuName = "Crafting/CraftingDatabase")]
public class CraftingDatabase : ScriptableObject
{
    public List<CraftRecipe> recipes = new List<CraftRecipe>();

    public CraftRecipe GetRecipeById(int id)
    {
        return recipes.Find(r => r.id == id);
    }

    public void ResetAllRecipeUnlocks()
    {
        foreach (var recipe in recipes)
            recipe.isUnlocked = false;
    }
}