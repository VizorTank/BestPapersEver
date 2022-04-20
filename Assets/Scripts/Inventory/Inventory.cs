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
    public GameObject CraftingUI;
    public List<UIItemSlot> ContainerSlots;
    public List<int> CraftingItemsSlot;
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
        for (int i = 0; i < ItemMenager.AmountofItems(); i++)
        {
            CraftingItemsSlot.Add(0);
        }
        foreach(ItemSlot slot in slots)
        {
            if (slot.HasItem)
            {
                CraftingItemsSlot[slot.stack.Item.id] += slot.stack.amount;
            }
        }

        int a = 0;
    }

   public ItemStack CraftItem(Item item)
   {
        foreach(CraftinReci reci in RecipeMenager.GetRecipe(item.id).CraftinRecipe)
        {
            FindAndTake(new ItemStack(ItemMenager.GetItem(reci.ID) , reci.Amount));
        }
        CraftingGrid();
        return new ItemStack(item, RecipeMenager.GetRecipe(item.id).AmountCrafted);
       
   }

    public bool isCraftable(Item item)
    {
        foreach(CraftinReci reci in RecipeMenager.GetRecipe(item.id).CraftinRecipe)
        {
            if (reci.Amount > CraftingItemsSlot[reci.ID]) return false;
        }
        return true;
    }

    public void CraftUntil(ItemStack itemStack, Item item)
    {

        for(; itemStack.amount + RecipeMenager.GetRecipe(item.id).AmountCrafted<= item.maxstack;)
        {
            if (!isCraftable(item)) break;
            itemStack.amount += CraftItem(item).amount;
        }
    }

    public void CraftMax(ItemStack itemStack, Item item)
    {
        CraftingRecipe recipe = RecipeMenager.GetRecipe(item.id);
        int itemamount = int.MaxValue;
        foreach(CraftinReci reci in recipe.CraftinRecipe)
        {
            int crafted = CraftingItemsSlot[reci.ID] / reci.Amount;
            if (crafted < itemamount) itemamount = crafted;
        }

        if (itemamount * recipe.AmountCrafted >= item.maxstack)
        {
            itemamount = item.maxstack;

            itemamount = (itemamount - itemStack.amount) / recipe.AmountCrafted;
        }

        foreach(CraftinReci reci in recipe.CraftinRecipe)
        {
            FindAndTake(new ItemStack(ItemMenager.GetItem(reci.ID), reci.Amount * itemamount));
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
            
            Item item = ItemMenager.GetItem(recipe.CraftedID);
            if (!isCraftable(item)) continue;

            GameObject newSlot = Instantiate(slotPrefab, CraftingUI.transform);
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), "Crafting");
            slot.isCrafting = true;
            slot.InsertStack(new ItemStack(item, 1));
        }

    }
    
}
