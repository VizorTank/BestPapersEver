using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Equipable", menuName = "Inventory/Item/Equipable")]
public class Equipable : Item
{
    public List<Stats> ItemStats;
}