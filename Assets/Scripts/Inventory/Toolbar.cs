using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbar : MonoBehaviour {

    public UIItemSlot[] slots;
    public RectTransform highlight;
    //public Player player;
    public int slotIndex = 0;
    public Item item;
    public Inventory inv;
    public PlayerInput playerInput;
    private void Start()
    {
        playerInput = new PlayerInput();
        playerInput.Enable();
    }
    private void OnDestroy()
    {
        playerInput.Disable();
    }

    private void Update() {

        
        float scroll = playerInput.Player.SelectHotbarSlot.ReadValue<float>();
        if (scroll != 0) {

            if (scroll > 0)
                slotIndex--;
            else
                slotIndex++;

            if (slotIndex > slots.Length - 1)
                slotIndex = 0;
            if (slotIndex < 0)
                slotIndex = slots.Length - 1;

            highlight.position = slots[slotIndex].slotIcon.transform.position;

        }
            

    }
    public void UseItem()
    {
        if(slots[slotIndex].HasItem)
        {
            Debug.Log(slots[slotIndex].itemSlot.stack.Item.name + " " + slots[slotIndex].itemSlot.stack.amount);

        }
        else
        {
            Debug.Log("no item");
        }
    }

}
