using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbar : MonoBehaviour
{

    public UIItemSlot[] slots;
    public RectTransform highlight;
    //public Player player;
    public int slotIndex = 0;
    public Item item;
    public Inventory inv;
    public PlayerInput playerInput;
    public PlayerController playerController;
    private void Start()
    {
        playerInput = new PlayerInput();
        playerInput.Enable();
    }
    private void OnDestroy()
    {
        playerInput.Disable();
    }

    private void Update()
    {


        float scroll = playerInput.Player.SelectHotbarSlot.ReadValue<float>();
        if (scroll != 0)
        {

            if (scroll > 0)
                slotIndex--;
            else
                slotIndex++;

            if (slotIndex > slots.Length - 1)
                slotIndex = 0;
            if (slotIndex < 0)
                slotIndex = slots.Length - 1;

            highlight.position = slots[slotIndex].slotIcon.transform.position;
            if (slots[slotIndex].itemSlot.HasItem)
                playerController.ItemType = slots[slotIndex].itemSlot.stack.Item.itemtype;
            else
                playerController.ItemType = Itemtype.None;
        }


    }
    public void UseItem(bool IsAbleToPlace)
    {
        if (slots[slotIndex].HasItem)
        {
            if (IsAbleToPlace&&slots[slotIndex].itemSlot.stack.Item.itemtype==Itemtype.Placeable)
            {
                playerController.PlaceBlock(slots[slotIndex].itemSlot.stack.Item.id);
                slots[slotIndex].itemSlot.Take(1);
            }
            

        }

    }


    public bool IsPlaceable()
    {
        return true;
     //  if (slots[slotIndex].HasItem)
     //  {
     //      if (slots[slotIndex].itemSlot.stack.Item.isPlaceable)
     //      {
     //          return slots[slotIndex].itemSlot.stack.Item.isPlaceable;
     //      }
     //  }
     //  return false;

    }
}



