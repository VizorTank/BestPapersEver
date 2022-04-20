using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour {

    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;
    [SerializeField] private EventSystem m_EventSystem = null;

    public Inventory inventory;
    PlayerInput playerInput;
    PlayerController player;

    private void Start() {

        player = GameObject.Find("Player").GetComponent<PlayerController>();
        playerInput = new PlayerInput();
        cursorItemSlot = new ItemSlot(cursorSlot);
        inventory = transform.GetComponent<Inventory>();
        playerInput.Enable();
    }


    private void Update() {

        if (!player.inInventory)
            return;


        cursorSlot.transform.position = playerInput.UI.Point.ReadValue<Vector2>();

        if (playerInput.Player.DestroyBlock.triggered) 
        {
            HandleSlotLeftClick(CheckForSlot());
        }
        if (playerInput.Player.PlaceBlock.triggered)
        {
            HandleSlotRightClick(CheckForSlot());
        }

    }

    private void HandleSlotLeftClick (UIItemSlot clickedSlot) {
        
        if (clickedSlot == null)
            return;

        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
            return;

        if (clickedSlot.itemSlot.isCrafting)
        {
            if(playerInput.Player.InventoryAdv.triggered && clickedSlot.itemSlot.isCrafting && !cursorItemSlot.HasItem)
            {
                cursorItemSlot.InsertStack(inventory.CraftItem(clickedSlot.itemSlot.stack.Item));
                inventory.CraftMax(cursorItemSlot.stack, clickedSlot.itemSlot.stack.Item);
            }
            else if(playerInput.Player.InventoryAdv.triggered && clickedSlot.itemSlot.isCrafting && cursorItemSlot.HasItem && cursorItemSlot.stack.Item == clickedSlot.itemSlot.stack.Item)
            {
                inventory.CraftMax(cursorItemSlot.stack, clickedSlot.itemSlot.stack.Item);
            }

            else if (clickedSlot.itemSlot.isCrafting && !cursorItemSlot.HasItem)
            {
                cursorItemSlot.InsertStack(inventory.CraftItem(clickedSlot.itemSlot.stack.Item));
            }
            else if (clickedSlot.itemSlot.isCrafting && cursorItemSlot.HasItem && cursorItemSlot.stack.Item == clickedSlot.itemSlot.stack.Item)
            {
                cursorItemSlot.stack.amount += inventory.CraftItem(clickedSlot.itemSlot.stack.Item).amount;
            }
            cursorItemSlot.UpdateSlot();
        }

        else
        {
             if (player.inContainer && playerInput.Player.InventoryAdv.triggered && clickedSlot.HasItem)
            {
                if (clickedSlot.itemSlot.SlotType == "Container")
                {
                    inventory.PickUpItem(clickedSlot.itemSlot.TakeAll(), "Backpack");
                    
                }

                else if (clickedSlot.itemSlot.SlotType != "Container")
                {
                    inventory.ToContainer(clickedSlot.itemSlot.TakeAll());
                    
                }
            }

            else if (playerInput.Player.InventoryAdv.triggered && clickedSlot.HasItem)
            {
                if (clickedSlot.itemSlot.SlotType == "Toolbar")
                {
                    inventory.PickUpItem(clickedSlot.itemSlot.TakeAll(), "Backpack");
                }
                else if (clickedSlot.itemSlot.SlotType == "Backpack")
                {
                    inventory.PickUpItem(clickedSlot.itemSlot.TakeAll(), "Toolbar");
                }
            }

            //podniesienie całego staku
            else if (!cursorSlot.HasItem && clickedSlot.HasItem)
            {

                cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());

            }


            //na pusty slot
            else if (cursorSlot.HasItem && !clickedSlot.HasItem)
            {

                clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());

            }
            //Dostakowywanie
            else if (cursorSlot.itemSlot.stack.Item == clickedSlot.itemSlot.stack.Item && clickedSlot.itemSlot.stack.amount < clickedSlot.itemSlot.stack.Item.maxstack)
            {
                if (cursorSlot.itemSlot.stack.amount <= clickedSlot.itemSlot.stack.Item.maxstack - clickedSlot.itemSlot.stack.amount)
                {
                    clickedSlot.itemSlot.stack.amount += cursorSlot.itemSlot.stack.amount;
                    cursorItemSlot.TakeAll();
                }
                else
                {
                    cursorSlot.itemSlot.stack.amount -= (clickedSlot.itemSlot.stack.Item.maxstack - clickedSlot.itemSlot.stack.amount);
                    clickedSlot.itemSlot.stack.amount = clickedSlot.itemSlot.stack.Item.maxstack;
                }
                clickedSlot.UpdateSlot();
                cursorSlot.UpdateSlot();

            }
            //Podmiana stacków
            if (cursorSlot.HasItem && clickedSlot.HasItem)
            {

                if (cursorSlot.itemSlot.stack.Item != clickedSlot.itemSlot.stack.Item)
                {

                    ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                    ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                    clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                    cursorSlot.itemSlot.InsertStack(oldSlot);

                }

            }
            inventory.CraftingGrid();
        }

    }

    
    private void HandleSlotRightClick(UIItemSlot clickedSlot)
    {
        if (clickedSlot == null)
            return;

        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
            return;

        if (clickedSlot.itemSlot.isCrafting)
        {
            if (clickedSlot.itemSlot.isCrafting && !cursorItemSlot.HasItem)
            {
                cursorItemSlot.InsertStack(inventory.CraftItem(clickedSlot.itemSlot.stack.Item));
            }
            else if (clickedSlot.itemSlot.isCrafting && cursorItemSlot.HasItem && cursorItemSlot.stack.Item == clickedSlot.itemSlot.stack.Item)
            {
                cursorItemSlot.stack.amount += inventory.CraftItem(clickedSlot.itemSlot.stack.Item).amount;
            }
            cursorItemSlot.UpdateSlot();
        }

        //na pusty slot
        else if (cursorSlot.HasItem && !clickedSlot.HasItem) {

            clickedSlot.itemSlot.InsertStack(new ItemStack(cursorItemSlot.stack.Item,1));
            cursorItemSlot.stack.amount--;
            if(cursorItemSlot.stack.amount==0)
            {
                cursorItemSlot.TakeAll();
            }
            clickedSlot.UpdateSlot();
            cursorSlot.UpdateSlot();

        }
        
        else if (!cursorSlot.HasItem && clickedSlot.HasItem)
        {
            if (playerInput.Player.InventoryAdv.triggered)
            {
                clickedSlot.itemSlot.stack.amount--;
                if (clickedSlot.itemSlot.stack.amount == 0)
                {
                    clickedSlot.itemSlot.TakeAll();
                }
                cursorItemSlot.InsertStack(new ItemStack(clickedSlot.itemSlot.stack.Item, 1));
            }
            else
            {
                if (clickedSlot.itemSlot.stack.amount > 1)
                {
                    int i = clickedSlot.itemSlot.stack.amount / 2;
                    cursorItemSlot.InsertStack(new ItemStack(clickedSlot.itemSlot.stack.Item, i));
                    clickedSlot.itemSlot.stack.amount -= i;
                    
                }
                else
                {
                    cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
                }
                
            }
            clickedSlot.UpdateSlot();
            cursorSlot.UpdateSlot();

        }

        //Dostakowywanie
        else if (cursorSlot.itemSlot.stack.Item == clickedSlot.itemSlot.stack.Item && clickedSlot.itemSlot.stack.amount< clickedSlot.itemSlot.stack.Item.maxstack) 
        {
            if (!playerInput.Player.InventoryAdv.triggered)
            {
                clickedSlot.itemSlot.stack.amount++;
                cursorItemSlot.stack.amount--;
                if (cursorItemSlot.stack.amount == 0)
                {
                    cursorItemSlot.TakeAll();
                }
            }
            else
            {
                clickedSlot.itemSlot.stack.amount--;
                if (clickedSlot.itemSlot.stack.amount == 0)
                {
                    clickedSlot.itemSlot.TakeAll();
                }
                cursorItemSlot.stack.amount++;
            }
            clickedSlot.UpdateSlot();
            cursorSlot.UpdateSlot();
        }

        inventory.CraftingGrid();
    }

    private UIItemSlot CheckForSlot () {

        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = playerInput.UI.Point.ReadValue<Vector2>();

        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);

        foreach (RaycastResult result in results) {

            if (result.gameObject.tag == "UIItemSlot")
                return result.gameObject.GetComponent<UIItemSlot>();
        }

        return null;

    }

}
