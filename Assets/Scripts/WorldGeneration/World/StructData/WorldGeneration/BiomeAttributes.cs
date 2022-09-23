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

    public int WaterLevel = 64;

    // public AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

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
            // lodes = new NativeArray<LodeStruct>(lodes.Count, Allocator.Persistent),

            treeThreshold = treeThreshold,
            treeMaxCount = treeMaxCount,

            // waterLevel = WaterLevel
        };

        // for (int i = 0; i < lodes.Count; i++)
        // {
        //     biome.lodes[i] = lodes[i].GetLodeStruct();
        // }
        return biome;
    }

    public NativeArray<LodeStruct> GetBiomeLodes()
    {
        if (_lodeStructs == null || _lodeStructs.Length != lodes.Count)
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

    // public int waterLevel;

    // public NativeArray<LodeStruct> lodes;

    // public int this[float3 position]
    // {
    //     get
    //     {
    //         int voxelValue = FirstPass(position);
    //         // if (voxelValue == 2)
    //         //     voxelValue = SecondPass(position, voxelValue);

    //         return voxelValue;
    //     }
    // }
    

    // public int GetBlock(float3 position)
    // {
    //     return GetBlock(position, CalculateTerrainHeight(position));
    // }

    public int GetBlock(float3 position, int terrainHeight, NativeArray<LodeStruct> lodes, int waterLevel)
    {
        int voxelValue = FirstPass(position, terrainHeight, waterLevel);
        if (voxelValue == 2)
            voxelValue = SecondPass(position, voxelValue, lodes);

        return voxelValue;
    }
    // public int GetBlock(float3 position, int terrainHeight, int waterLevel)
    // {
    //     int voxelValue = FirstPass(position, terrainHeight, waterLevel);

    //     return voxelValue;
    // }


    public int CalculateTerrainHeight(float3 position)
    {
        // int3 a = new int3(position) - new int3(position) % VoxelData.ChunkSize;
        // a /= VoxelData.ChunkSize;
        // int result = solidGroundHeight;

        // if (a.x % 2 == 0)
        //     result += (int)position.x % VoxelData.ChunkSize.x;
        // else
        //     result += VoxelData.ChunkSize.x - (int)position.x % VoxelData.ChunkSize.x;
        
        // return result;

        // return solidGroundHeight + (int)(position.z) % VoxelData.ChunkSize.x;// + ((int)position.z % VoxelData.ChunkSize.z);

        return (int)math.floor((noise.snoise(
                new float2(
                    position.x / chunkSize.x * terrainSize + offset,
                    position.z / chunkSize.z * terrainSize + offset)) + 1.0) / 2 * terrainHeightDifference) + solidGroundHeight;
    }

    // public int FirstPass(float3 position)
    // {
    //     return FirstPass(position, CalculateTerrainHeight(position),0);
    // }

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

// [System.Serializable]
// public class Lode
// {
//     public string name;
//     public int blockID;
//     public int minHeight;
//     public int maxHeight;
//     public float scale;
//     public float threshold;
//     public float noiseOffset;

//     public LodeStruct GetLodeStruct()
//     {
//         return new LodeStruct
//         {
//             blockID = blockID,
//             maxHeight = maxHeight,
//             minHeight = minHeight,
//             noiseOffset = noiseOffset,
//             scale = scale,
//             threshold = threshold
//         };
//     }
// }

// public struct LodeStruct
// {
//     public int blockID;
//     public int minHeight;
//     public int maxHeight;
//     public float scale;
//     public float threshold;
//     public float noiseOffset;

//     public bool CheckForLode(float3 position)
//     {
//         return noise.snoise((position + noiseOffset) * scale) > threshold;
//     }
// }