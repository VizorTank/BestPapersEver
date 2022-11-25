using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockTypesList", menuName = "CustomObjects/Block Types List")]
public class BlockTypesList : ScriptableObject
{
    public List<BlockType> blockTypes;
    public List<Texture2D> textures;

    public List<string> Names { get => names; }
    private List<string> names;
    public NativeArray<bool> areSolid;
    public NativeArray<bool> areTransparent;
    public int[] areTransparentInt;
    public NativeArray<bool> areInvisible;
    public NativeArray<bool> areReplacable;
    public NativeArray<bool> areLiquid;

    public int BlockCount;
    public List<Material> Materials { get => materials; }
    private List<Material> materials;

    public Texture2DArray TextureArray;

    public Material Material;
    public ComputeShader Blocks;
    public ComputeShader Culling;

    public ComputeBuffer BlocksIsTransparentBuffer;
    public Mesh Mesh;
    public Material MTest;
    public Material MTest2;

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
        Texture texture = textures[1];
        TextureArray = new Texture2DArray(texture.width, texture.height, blockTypes.Count - 1, TextureFormat.RGBA32, false);
        TextureArray.filterMode = FilterMode.Point;

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
            if (BlockCount != 0)
                TextureArray.SetPixels(textures[BlockCount].GetPixels(), BlockCount - 1);

            BlockCount++;
        }
        TextureArray.Apply();
        BlocksIsTransparentBuffer.SetData(areTransparentInt);
    }

    public void Destroy()
    {
        areSolid.Dispose();
        areTransparent.Dispose();
        areInvisible.Dispose();
        areReplacable.Dispose();
        areLiquid.Dispose();
    }

    ~BlockTypesList()
    {
        Destroy();
    }
}
