using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Inventory/CraftingRecipe")]
public class CraftingRecipe : ScriptableObject
{
    public int CraftingID;
    public int CraftedID;
    public int AmountCrafted;
    public List<CraftinReci> CraftinRecipe;
}

[System.Serializable]
public class CraftinReci
{
    public int ID;
    public int Amount;
}

public class RecipeMenager
{
    static List<CraftingRecipe> Recipes = new List<CraftingRecipe>();

    static RecipeMenager()
    {
        Object[] items = Resources.LoadAll("Recipes", typeof(CraftingRecipe));
        foreach (CraftingRecipe item in items)
        {
            Recipes.Add(item);
        }
    }

    public static CraftingRecipe GetRecipe(int ItemID)
    {
        foreach (CraftingRecipe item in Recipes)
        {
            if (item.CraftedID == ItemID)
                return item;
        }
        return null;
    }


    public static int AmountofRecipes()
    {
        return Recipes.Count;
    }
    public static List<CraftingRecipe> GetRecipesList()
    {
        return Recipes;
    }
}
