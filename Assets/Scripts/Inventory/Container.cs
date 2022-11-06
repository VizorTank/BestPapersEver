using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
    List<ItemSlot> slots = new List<ItemSlot>();

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 1; i <= 27; i++)
        {

            ItemSlot slot = new ItemSlot("Container");
            slots.Add(slot);

        }
    }

    public List<ItemSlot> GetContainer()
    {
        return slots;
    }

}
