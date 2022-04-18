using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "CustomObjects/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    public string biomeName;
    public int solidGroundHeight;
    public int terrainHeightDifference;
    public float terrainSize;

    public Lode[] lodes;

    public BiomeAttributesStruct GetBiomeStruct(float seed = 0)
    {
        BiomeAttributesStruct biome = new BiomeAttributesStruct
        {
            solidGroundHeight = solidGroundHeight,
            terrainHeightDifference = terrainHeightDifference,
            terrainSize = terrainSize,
            chunkSize = VoxelData.ChunkSize,
            offset = seed,
            lodes = new NativeArray<LodeStruct>(lodes.Length, Allocator.Persistent)
        };

        for (int i = 0; i < lodes.Length; i++)
        {
            biome.lodes[i] = lodes[i].GetLodeStruct();
        }
        return biome;
    }
}

public struct BiomeAttributesStruct
{
    public int solidGroundHeight;
    public int terrainHeightDifference;
    public float terrainSize;
    public int3 chunkSize;
    public float offset;

    public NativeArray<LodeStruct> lodes;

    public int this[float3 position]
    {
        get
        {
            int voxelValue = FirstPass(position);
            if (voxelValue == 2)
                voxelValue = SecondPass(position, voxelValue);

            return voxelValue;
        }
    }

    public int GetBlock(float3 position)
    {
        int voxelValue = FirstPass(position);
        if (voxelValue == 2)
            voxelValue = SecondPass(position, voxelValue);

        return voxelValue;
    }

    public int CalculateTerrainHeight(float3 position)
    {
        return (int)math.floor((noise.snoise(
                new float2(
                    position.x / chunkSize.x * terrainSize + offset,
                    position.z / chunkSize.z * terrainSize + offset)) + 1.0) / 2 * terrainHeightDifference) + solidGroundHeight;
    }

    public int FirstPass(float3 position)
    {
        int voxelValue = 0;

        int waterLevel = 64;

        int yPos = (int)math.floor(position.y);
        int terrainHeight = CalculateTerrainHeight(position);

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

        return voxelValue;
    }

    public int SecondPass(float3 position, int voxelValue)
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

[System.Serializable]
public class Lode
{
    public string name;
    public int blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;

    public LodeStruct GetLodeStruct()
    {
        return new LodeStruct
        {
            blockID = blockID,
            maxHeight = maxHeight,
            minHeight = minHeight,
            noiseOffset = noiseOffset,
            scale = scale,
            threshold = threshold
        };
    }
}

public struct LodeStruct
{
    public int blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;

    public bool CheckForLode(float3 position)
    {
        return noise.snoise((position + noiseOffset) * scale) > threshold;
    }
}