using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Inventory/Item/Tool")]
public class Tool : Item
{
    public ToolType tooltype;
    public int ToolPower;
}
