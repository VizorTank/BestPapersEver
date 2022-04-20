using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemStack {

    public Item Item;
    public int amount;

    public ItemStack (Item item, int _amount) {

        Item = item;
        amount = _amount;

    }

}
