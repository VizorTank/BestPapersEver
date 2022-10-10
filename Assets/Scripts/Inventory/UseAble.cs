using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Useable", menuName = "Inventory/Item/UseAble")]
public class UseAble : Item
{
    public Action UseItem;
}
