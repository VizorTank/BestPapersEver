using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode 
{
    public int x;
    public int y;
    public int z;

    public int gCost;
    public int hCost;
    public int fCost;

    public bool IsWalkable;
    public PathNode cameFromNode;

    public PathNode(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = y;
        IsWalkable = true;
    }


    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }
}
