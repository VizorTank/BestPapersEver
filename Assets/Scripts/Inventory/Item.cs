using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item/Material")]
public class Item : ScriptableObject
{
    public string NameID;
    public int IntID=0;
    public int maxstack;
    public string ItemName;
    public Sprite image;
    public Itemtype itemtype = Itemtype.Material;
}

public enum ArmorType
{
    Helmet,
    ChestPlate,
    Leggings,
    Boots
}

public enum ToolType
{
    Pickaxe,
    Axe,
    Shovel, //???
    Hoe
}


public class ItemMenager
{
    static List<Item> Items = new List<Item>();

    //turn on after adding new item, nesseary for saving
    bool setID = false;


    static ItemMenager _instance;
    public static ItemMenager GetInstance()
    {
        if (_instance == null)
        {
            _instance = new ItemMenager();
        }
        return _instance;
    }
    private ItemMenager()
    {
        int Setter = 1;
        Items.Clear();
        UnityEngine.Object[] items = Resources.LoadAll("Items", typeof(Item));
        foreach (Item item in items)
        {
            Items.Add(item);
            if(setID)
            {
                item.IntID = Setter;
                Setter++;
            }
        }

        
    }
    
    public static void Destroy()
    {
        _instance = null;
    }

    public static Item GetItem(string ItemName)
    {

        return Items.Find(x => x.NameID == ItemName);

    }

    public static Item GetItem(int ItemID)
    {

        return Items.Find(x => x.IntID == ItemID);

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


public enum Itemtype
{
    Building,
    Useable,
    Tool,
    Weapon,
    Equipable,
    Material,
    None
};