using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Inventory/CraftingRecipe")]
[System.Serializable]
public class CraftingRecipe
{
    public string name;
    public string ItemName;
    public int AmountCrafted;
    public CraftingLevel CraftingLevel = CraftingLevel.None;
    public List<CraftinReci> CraftinRecipe;
}

[System.Serializable]
public class CraftinReci
{
    public string ItemName;
    public int Amount;


    public CraftinReci(string ItemName, int Amount)
    {
        this.ItemName = ItemName;
        this.Amount = Amount;
    }
}


public class RecipeMenager
{
    static List<CraftingRecipe> Recipes = new List<CraftingRecipe>();
    static RecipeMenager _instance;
    public static RecipeMenager GetInstance()
    {
        if(_instance == null)
        {
            _instance = new RecipeMenager();
        }
        return _instance;
    }


    private RecipeMenager()
    {
        Recipes.Clear();
        Object[] items = Resources.LoadAll("Recipes");
        foreach (CraftingList item in items)
        {
            foreach (CraftingRecipe recipe in item.Recipes)
            {
                
                Recipes.Add(recipe);
                
            }

        }
    }

    public static void Destroy()
    {
        _instance = null;
    }

    ~RecipeMenager()
    {
        Recipes.Clear();
    }

    public static CraftingRecipe GetRecipe(string RecName)
    {
       return Recipes.Find(x => x.ItemName == RecName);
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

public enum CraftingLevel
{
    None,
    CraftingLV1,
    CraftingLV2,
    FurnaceLV1,
    FurnaceLV2,
    AnvilLV1,
    AnvilLV2
}
