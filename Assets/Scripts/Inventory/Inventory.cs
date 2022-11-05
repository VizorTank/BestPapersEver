using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour {

    public GameObject slotPrefab;
    public UIItemSlot[] toolbaruislots;
    public List<ItemSlot> slots = new List<ItemSlot>();
    public GameObject Backpack;
    public GameObject cursorSlot;
    public GameObject Toolbar;
    public GameObject ContainerUI;
    public GameObject Equipment;
    public GameObject InventoryUI;
    public List<UIItemSlot> ContainerSlots;
    public List<CraftinReci> CraftingItemsSlot;



    [Header("Crafting")]
    public GameObject CraftingUI;
    public GameObject CraftingWindow;
    public GameObject CraftingPreviev;
    public UIItemSlot CraftingPrevievSlot;
    public CraftingRecipe SelectedCraft = null;




    [Header("Equipment")]
    public List<UIItemSlot> equipmentSlots;
    public UIItemSlot HelmetSlot;
    public UIItemSlot ChestplateSlot;
    public UIItemSlot LeggingsSlot;
    public UIItemSlot BootsSlot;
    private void Start() {

        foreach (UIItemSlot slot in toolbaruislots)
        {
            if(slot.itemSlot==null)
            slot.itemSlot = new ItemSlot(slot,"Toolbar");
            slots.Add(slot.itemSlot);
        }
        for (int i = 1; i <= 27; i++) {

            GameObject newSlot = Instantiate(slotPrefab, Backpack.transform);
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(),"Backpack");
            slots.Add(slot);

        }
        for (int i = 1; i <= 27; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, ContainerUI.transform);
            ContainerSlots.Add(newSlot.GetComponent<UIItemSlot>());
        }

        foreach (UIItemSlot slot in equipmentSlots)
        {
            if (slot.itemSlot == null)
                slot.itemSlot = new ItemSlot(slot);
            slot.itemSlot.slotType = UISlotType.EqSlot;
        }

        HelmetSlot.itemSlot = new ItemSlot(HelmetSlot);
        HelmetSlot.itemSlot.slotType = UISlotType.Helmet;
        equipmentSlots.Add(HelmetSlot);

        ChestplateSlot.itemSlot = new ItemSlot(ChestplateSlot);
        ChestplateSlot.itemSlot.slotType = UISlotType.ChestPlate;
        equipmentSlots.Add(ChestplateSlot);

        LeggingsSlot.itemSlot = new ItemSlot(LeggingsSlot);
        LeggingsSlot.itemSlot.slotType = UISlotType.Leggings;
        equipmentSlots.Add(LeggingsSlot);

        BootsSlot.itemSlot = new ItemSlot(BootsSlot);
        BootsSlot.itemSlot.slotType = UISlotType.Boots;
        equipmentSlots.Add(BootsSlot);

        CraftingPrevievSlot.itemSlot = new ItemSlot(CraftingPrevievSlot);
        CraftingPrevievSlot.itemSlot.slotType = UISlotType.CraftingPreviev;

        SelectedCraft = RecipeMenager.GetRecipe("Planks");
        CraftingGrid();
    }

    public void OpenInventory()
    {
        InventoryUI.SetActive(true);
        Backpack.SetActive(true);
        cursorSlot.SetActive(true);
    }

    public void CloseInventory()
    {
        InventoryUI.SetActive(false);
        Backpack.SetActive(false);
        cursorSlot.SetActive(false);
        CraftingWindow.SetActive(false);
        Equipment.SetActive(false);
    }
    public void ChangeHighlightedCrafting(CraftingRecipe select)
    {
        SelectedCraft = select;

        UpdatePreviev();

    }
    public void UpdatePreviev()
    {
        foreach (Transform child in CraftingPreviev.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        foreach (CraftinReci reci in SelectedCraft.CraftinRecipe)
        {

            Item item = ItemMenager.GetItem(reci.ItemName);

            GameObject newSlot = Instantiate(slotPrefab, CraftingPreviev.transform);
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), "Crafting");
            slot.slotType = UISlotType.Other;
            slot.InsertStack(new ItemStack(item, reci.Amount));
            string message = CraftingItemsSlot.Find(x=> x.ItemName==reci.ItemName).Amount + "/" + reci.Amount;
            slot.UpdateSlot(message,!( reci.Amount > CraftingItemsSlot.Find(x => x.ItemName == reci.ItemName).Amount));
        }
        CraftingPrevievSlot.itemSlot.InsertStack(new ItemStack(ItemMenager.GetItem(SelectedCraft.ItemName), SelectedCraft.AmountCrafted));
        CraftingPrevievSlot.UpdateSlot();
    }

    public void OpenCrafting()
    {
        Equipment.SetActive(false);
        CraftingWindow.SetActive(!CraftingWindow.activeSelf);
    }
    public void OpenEqupment()
    {
        CraftingWindow.SetActive(false);
        Equipment.SetActive(!Equipment.activeSelf);
    }

    public ItemStack PickUpItem(ItemStack item, string _SlotType = null)
    {
        foreach (ItemSlot slot in slots)
        {
            if (slot.SlotType == _SlotType || _SlotType==null)
            {
                item = Stacking(item, slot);
            }
        }
        CraftingGrid();
        Toolbar.GetComponent<Toolbar>().UpdateHander();
        return item;
    }

    public ItemStack ToContainer(ItemStack item)
    {
        foreach (UIItemSlot slota in ContainerSlots)
        {
            ItemSlot slot = slota.itemSlot;
            item = Stacking(item, slot);
        }
        return item;
    }

    public ItemStack Stacking(ItemStack item, ItemSlot slot)
    {
        if (slot.HasItem && slot.stack.Item == item.Item && slot.stack.amount < slot.stack.Item.maxstack)
        {
            if (item.amount <= (slot.stack.Item.maxstack - slot.stack.amount))
            {
                slot.stack.amount += item.amount;
                slot.UpdateSlot();
                item.amount = 0;
                return item;
            }
            else
            {
                item.amount -= (slot.stack.Item.maxstack - slot.stack.amount);
                slot.stack.amount = slot.stack.Item.maxstack;
                slot.UpdateSlot();
                return item;
            }
        }
        if (!slot.HasItem && item.amount != 0)
        {
            slot.InsertStack(item);
            slot.UpdateSlot();

            return new ItemStack(item.Item, 0);
        }
        return item;
    }

    //obsługo kontenerów
    public void OpenContainer(Container container)
    {
        int i = 0;
        foreach (ItemSlot slot in container.GetContainer())
        {
            slot.LinkUISlot(ContainerSlots[i]);
            ContainerSlots[i].Link(slot);
            i++;
        }
    }
    public void CloseContainer(Container container)
    {
        int i = 0;
        foreach (ItemSlot slot in container.GetContainer())
        {
            slot.unLinkUISlot();
            ContainerSlots[i].UnLink();
            i++;
        }
    }

    public void FindAndTake(ItemStack item)
    {
        foreach(ItemSlot slot in slots)
        {
            if(slot.HasItem && slot.stack.Item==item.Item)
            {
                if(slot.stack.amount<=item.amount)
                {
                    item.amount -= slot.stack.amount;
                    slot.TakeAll();
                }
                else
                {
                    slot.stack.amount -= item.amount;
                    item.amount = 0;
                    slot.UpdateSlot();
                }
            }
            if (item.amount==0) break;
        }
    }

    public void CalculateInventory()
    {

       CraftingItemsSlot.Clear();
        foreach (Item item in ItemMenager.GetItemList())
        {
            CraftingItemsSlot.Add(new CraftinReci(item.NameID, 0));
        }
         foreach(ItemSlot slot in slots)
         {
             if (slot.HasItem)
             {
                CraftingItemsSlot.Find(x => x.ItemName == slot.stack.Item.NameID).Amount += slot.stack.amount;
             }
         }
    }

    public ItemStack CraftItem()
   {
        foreach(CraftinReci reci in SelectedCraft.CraftinRecipe)
        {
            FindAndTake(new ItemStack(ItemMenager.GetItem(reci.ItemName) , reci.Amount));
        }
        CraftingGrid();
        return new ItemStack(ItemMenager.GetItem(SelectedCraft.ItemName), SelectedCraft.AmountCrafted);
       
   }

    public bool isCraftable()
    {
        foreach(CraftinReci reci in SelectedCraft.CraftinRecipe)
        {
            if (reci.Amount > CraftingItemsSlot.Find(x => x.ItemName == reci.ItemName).Amount) return false;
        }
        return true;
    }

    public void CraftUntil(ItemStack itemStack, Item item)
    {

        for(; itemStack.amount + RecipeMenager.GetRecipe(item.NameID).AmountCrafted<= item.maxstack;)
        {
            if (!isCraftable()) break;
            itemStack.amount += CraftItem().amount;
        }
    }

    public void CraftMax(ItemStack itemStack )
    {
        CraftingRecipe recipe = SelectedCraft;
        Item item = ItemMenager.GetItem(recipe.ItemName);
        int itemamount = int.MaxValue;
        foreach(CraftinReci reci in recipe.CraftinRecipe)
        {
            int crafted = CraftingItemsSlot.Find(x => x.ItemName == reci.ItemName).Amount / reci.Amount;
            if (crafted < itemamount) itemamount = crafted;
        }

        if (itemamount * recipe.AmountCrafted >= item.maxstack)
        {
            itemamount = item.maxstack;

            itemamount = (itemamount - itemStack.amount) / recipe.AmountCrafted;
        }

        foreach(CraftinReci reci in recipe.CraftinRecipe)
        {
            FindAndTake(new ItemStack(ItemMenager.GetItem(reci.ItemName), reci.Amount * itemamount));
        }

        CraftingGrid();
        itemStack.amount = itemStack.amount + itemamount * recipe.AmountCrafted;

    }

    public void CraftingGrid()
    {
        
        CalculateInventory();
        foreach(Transform child in CraftingUI.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        foreach(CraftingRecipe recipe in RecipeMenager.GetRecipesList())
        {
            
           //Item item = ItemMenager.GetItem(recipe.CraftedID);
           //if (!isCraftable(item)) continue;

            GameObject newSlot = Instantiate(slotPrefab, CraftingUI.transform);
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), "Crafting");
            slot.slotType = UISlotType.Crafting;
            slot.InsertCraftng(recipe);
        }
        UpdatePreviev();
    }
    public List<Stats> GetItemStats()
    {
        List<Stats> output = new List<Stats>();

        foreach(UIItemSlot slot in equipmentSlots)
            if (slot.HasItem)
            foreach(Stats stat in ((Equipable)slot.itemSlot.stack.Item).ItemStats)
            {
                Stats stats = output.Find(x => x.StatName == stat.StatName);
                if (stats == null)
                {
                    Stats temp = new Stats(stat.StatName, stat.GetValue());
                    output.Add(temp);
                }
                else
                    stats.Value += stat.GetValue();
            }

        return output;
    }




    public int[,] GetItemsForSaving()
    {
        int[,] output = new int[ItemMenager.AmountofItems(), 2];
        int i = 0;
        foreach(ItemSlot item in slots)
        {

            if (item.HasItem)
            {
                output[i, 0] = item.stack.Item.IntID;
                output[i, 1] = item.stack.amount;
            }
            else
            {
                output[i, 0] = 0;
                output[i, 1] = 0;
            }
            i++;
        }

        return output;
    }

    public void LoadItems(int[,] items)
    {
        int i = 0;

        foreach(ItemSlot item in slots)
        {

            if(items[i, 0]!=0)
            item.InsertStack(new ItemStack(ItemMenager.GetItem(items[i, 0]), items[i, 1]));
            i++;
        }
    }

    public void TempClearInventory()
    {
        foreach (ItemSlot item in slots)
        {
            item.EmptySlot();
        }
    }

}

[System.Serializable]
public class Stats
{
    public StatEnum StatName;
    public float Value;
    private string fmt = "0.00";
    public Stats(StatEnum Name, float Value)
    {
        StatName = Name;
        this.Value = Value;
    }
    public float GetValue()
    {
        return Value;
    }
    public override string ToString()
    {
        return StatName.ToString() + " : " + Value.ToString(fmt);
    }
}

public enum StatEnum
{
    Armor,
    Damage,
    ColdResist,
    HeatResist,
    MagicResist,
    BonusHealth,
    BonusStamina
}
