using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockTypesList", menuName = "CustomObjects/Block Types List")]
public class BlockTypesList : ScriptableObject
{
    public List<BlockType> blockTypes;

    public List<string> Names { get => names; }
    private List<string> names;
    public NativeArray<bool> areSolid;
    public NativeArray<bool> areTransparent;
    public NativeArray<bool> areInvisible;
    public NativeArray<bool> areReplacable;

    public int BlockCount;
    public List<Material> Materials { get => materials; }
    private List<Material> materials;

    public void ProcessData()
    {
        names = new List<string>();
        materials = new List<Material>();

        areSolid = new NativeArray<bool>(blockTypes.Count, Allocator.Persistent);
        areTransparent = new NativeArray<bool>(blockTypes.Count, Allocator.Persistent);
        areInvisible = new NativeArray<bool>(blockTypes.Count, Allocator.Persistent);
        areReplacable = new NativeArray<bool>(blockTypes.Count, Allocator.Persistent);

        BlockCount = 0;
        foreach (BlockType blockType in blockTypes)
        {
            Names.Add(blockType.name);
            Materials.Add(blockType.material);

            areSolid[BlockCount] = blockType.isSolid;
            areTransparent[BlockCount] = blockType.isTransparent;
            areInvisible[BlockCount] = blockType.isInvisible;
            areReplacable[BlockCount] = blockType.isReplaceable;

            BlockCount++;
        }
    }

    public void Destroy()
    {
        areSolid.Dispose();
        areTransparent.Dispose();
        areInvisible.Dispose();
        areReplacable.Dispose();
    }

    ~BlockTypesList()
    {
        Destroy();
    }
}
