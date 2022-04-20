using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemInteractable : MonoBehaviour
{
    public Item item;
    public int amount;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            amount =other.gameObject.GetComponent<PlayerController>().PickUpItem(new ItemStack(item, amount)).amount;
        }
        else if(!(amount==item.maxstack) && other.gameObject.CompareTag("Item"))
        {
            ItemInteractable itemInteractable = other.gameObject.GetComponent<ItemInteractable>();
            if (item == itemInteractable.item && amount<item.maxstack && amount >= itemInteractable.amount)
            {
                if(itemInteractable.amount <= item.maxstack- amount)
                {
                    amount += itemInteractable.amount;
                    itemInteractable.amount = 0;
                    Destroy(other.gameObject);
                }
                else
                {
                    itemInteractable.amount -= item.maxstack - amount;
                    amount = item.maxstack;
                }
            }
        }
        if(amount==0)
        Destroy(gameObject);
    }
}
