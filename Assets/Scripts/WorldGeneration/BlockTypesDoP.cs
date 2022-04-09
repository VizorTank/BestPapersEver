using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class BlockTypesDoP
{
    public List<string> names = new List<string>();
    public NativeArray<bool> areSolid;
    public List<Material> materials = new List<Material>();

    public BlockTypesDoP(BlockType[] blockTypes)
    {
        areSolid = new NativeArray<bool>(blockTypes.Length, Allocator.Persistent);
        int i = 0;
        foreach (BlockType blockType in blockTypes)
        {
            names.Add(blockType.name);
            areSolid[i++] = blockType.isSolid;
            materials.Add(blockType.material);
        }
    }

    public void Destroy()
    {
        areSolid.Dispose();
    }

    ~BlockTypesDoP()
    {
        areSolid.Dispose();
    }
}
