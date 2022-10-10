using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New List", menuName = "Inventory/CraftingList")]
public class CraftingList : ScriptableObject
{
    public List<CraftingRecipe> Recipes;
}
