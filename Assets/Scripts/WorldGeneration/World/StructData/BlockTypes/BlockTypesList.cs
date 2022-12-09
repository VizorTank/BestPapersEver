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

    [HideInInspector] public int[] areTransparentInt;
    public NativeArray<bool> areInvisible;
    public NativeArray<bool> areReplacable;
    public NativeArray<bool> areLiquid;

    private int BlockCount;
    public List<Material> Materials { get => materials; }
    private List<Material> materials;
    public ComputeShader Blocks;
    public ComputeShader Culling;
    public ComputeShader Sorting;

    public ComputeBuffer BlocksIsTransparentBuffer;

    public void ProcessData()
    {
        names = new List<string>();
        materials = new List<Material>();
        

        areSolid = new NativeArray<bool>(blockTypes.Count, Allocator.Persistent);
        areTransparent = new NativeArray<bool>(blockTypes.Count, Allocator.Persistent);
        areTransparentInt = new int[blockTypes.Count];
        BlocksIsTransparentBuffer = new ComputeBuffer(blockTypes.Count, sizeof(int));
        
        areInvisible = new NativeArray<bool>(blockTypes.Count, Allocator.Persistent);
        areReplacable = new NativeArray<bool>(blockTypes.Count, Allocator.Persistent);

        areLiquid = new NativeArray<bool>(blockTypes.Count, Allocator.Persistent);

        BlockCount = 0;
        foreach (BlockType blockType in blockTypes)
        {
            Names.Add(blockType.name);
            Materials.Add(blockType.material);

            areSolid[BlockCount] = blockType.isSolid;
            areTransparent[BlockCount] = blockType.isTransparent;
            areTransparentInt[BlockCount] = blockType.isTransparent ? 1 : 0;
            areInvisible[BlockCount] = blockType.isInvisible;
            areReplacable[BlockCount] = blockType.isReplaceable;
            areLiquid[BlockCount] = blockType.isLiquid;

            BlockCount++;
        }
        BlocksIsTransparentBuffer.SetData(areTransparentInt);
    }

    public void Destroy()
    {
        areSolid.Dispose();
        areTransparent.Dispose();
        areInvisible.Dispose();
        areReplacable.Dispose();
        areLiquid.Dispose();

        BlocksIsTransparentBuffer.Dispose();
    }

    ~BlockTypesList()
    {
        Destroy();
    }
}
