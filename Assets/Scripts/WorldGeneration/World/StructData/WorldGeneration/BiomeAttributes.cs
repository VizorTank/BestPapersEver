using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "CustomObjects/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    public string biomeName;
    [Range(0, 1)]
    public float BiomeHeightThresholdMin;
    [Range(0, 1)]
    public float BiomeHeightThresholdMax;

    public int solidGroundHeight;
    public int terrainHeightDifference;
    public float terrainSize;

    public float treeThreshold;
    public int treeMaxCount;
    
    public List<Lode> lodes;
    private NativeArray<LodeStruct> _lodeStructs;

    public BiomeAttributesStruct GetBiomeStruct(float seed = 0)
    {
        BiomeAttributesStruct biome = new BiomeAttributesStruct
        {
            solidGroundHeight = solidGroundHeight,
            terrainHeightDifference = terrainHeightDifference,
            terrainSize = terrainSize,
            chunkSize = VoxelData.ChunkSize,
            offset = seed,

            treeThreshold = treeThreshold,
            treeMaxCount = treeMaxCount
        };
        return biome;
    }

    public NativeArray<LodeStruct> GetBiomeLodes()
    {
        if (!_lodeStructs.IsCreated)
        {
            _lodeStructs = new NativeArray<LodeStruct>(lodes.Count, Allocator.Persistent);
            for (int i = 0; i < lodes.Count; i++)
            {
                _lodeStructs[i] = lodes[i].GetLodeStruct();
            }
        }

        return _lodeStructs;
    }

    public void Destroy()
    {
        try { _lodeStructs.Dispose(); } catch { }
    }
}

public struct BiomeAttributesStruct
{
    public int solidGroundHeight;
    public int terrainHeightDifference;
    public float terrainSize;
    public int3 chunkSize;
    public float offset;

    public float treeThreshold;
    public float treeMaxCount;

    public int GetBlock(float3 position, int terrainHeight, NativeArray<LodeStruct> lodes, int waterLevel)
    {
        int voxelValue = FirstPass(position, terrainHeight, waterLevel);
        if (voxelValue == 2)
            voxelValue = SecondPass(position, voxelValue, lodes);

        return voxelValue;
    }

    public int CalculateTerrainHeight(float3 position)
    {
        return (int)math.floor(
            (noise.snoise(
                new float2(
                    position.x / chunkSize.x * terrainSize + offset,
                    position.z / chunkSize.z * terrainSize + offset)) + 1.0) 
                / 2 * terrainHeightDifference) + solidGroundHeight;
    }

    public int FirstPass(float3 position, int terrainHeight, int waterLevel)
    {
        int voxelValue = 0;

        int yPos = (int)math.floor(position.y);

        if (yPos > terrainHeight)
        {
            if (yPos > waterLevel)
                voxelValue = 0;
            else
                voxelValue = 6;
        }
        else if (yPos == terrainHeight)
        {
            if (yPos > waterLevel)
                voxelValue = 3;
            else
                voxelValue = 4;
        }
        else if (yPos > terrainHeight - 4)
            voxelValue = 4;
        else if (yPos < terrainHeight)
            voxelValue = 2;
        
        if (yPos == 0)
            voxelValue = 1;
        if (yPos < 0)
            voxelValue = 0;

        return voxelValue;
    }

    public int SecondPass(float3 position, int voxelValue, NativeArray<LodeStruct> lodes)
    {
        foreach (LodeStruct lode in lodes)
        {
            if (position.y >= lode.minHeight && position.y <= lode.maxHeight)
            {
                if (lode.CheckForLode(position)) voxelValue = lode.blockID;
            }
        }

        return voxelValue;
    }
}