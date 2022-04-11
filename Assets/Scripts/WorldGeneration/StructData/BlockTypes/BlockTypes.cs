using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class BlockType
{
    public string name;
    public Material material;
    public bool isSolid;
    public bool isTransparent;
    public bool isLiquid;
    public bool isInvisible;

    public bool isFalling;
    public bool isGlowing;

    public bool isGrowing;

    public List<int> DropTable;
}
