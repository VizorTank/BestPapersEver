using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    //public string name;
    public int id;
    public int maxstack;
    public string ItemName;
    public Sprite image;
    public bool isPlaceable;
    public Itemtype itemtype;
}






public class ItemMenager
{
    static List<Item> Items = new List<Item>();
    
    static ItemMenager()
    {
        Object[] items = Resources.LoadAll("Items", typeof(Item));
        foreach (Item item in items)
        {
            Items.Add(item);
        }
    }
    
    public static Item GetItem(string ItemName)
    {
        foreach (Item item in Items)
        {
            if (item.name.CompareTo(ItemName) == 0)
                return item;
        }
        return null;
    }

    public static Item GetItem(int ItemID)
    {
        foreach (Item item in Items)
        {
            if (item.id == ItemID)
                return item;
        }
        return null;
    }
    public static int AmountofItems()
    {
        return Items.Count;
    }
    public static List<Item> GetItemList()
    {
        return Items;
    }
}