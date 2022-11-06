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
            UpdateHander();
        }
    }
    public void UseItem(bool IsAbleToPlace)
    {
        if (slots[slotIndex].HasItem)
        {
            if (IsAbleToPlace&& slots[slotIndex].itemSlot.stack.Item is Placeable)
            {
                playerController.PlaceBlock(((Placeable)slots[slotIndex].itemSlot.stack.Item).BlockID);
                slots[slotIndex].itemSlot.Take(1);
            }
        }
    }

    public void UpdateHander()
    {
        if (slots[slotIndex].itemSlot.HasItem)
            playerController.SelectItem = slots[slotIndex].itemSlot.stack.Item;
        else
            playerController.SelectItem = null;
    }

}



