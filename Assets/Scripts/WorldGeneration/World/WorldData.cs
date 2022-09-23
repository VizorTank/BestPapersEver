using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;

[System.Serializable]
public class WorldData
{
    public int[,] chunks;

    public WorldData(Dictionary<int3, IChunk> createdChunk)
    {
        chunks = new int[createdChunk.Count, 3];
        int i = 0;
        foreach (var item in createdChunk)
        {
            chunks[i, 0] = item.Key.x;
            chunks[i, 1] = item.Key.y;
            chunks[i, 2] = item.Key.z;
            i++;
        }
    }

    internal Dictionary<int3, IChunk> GetGeneratedChunks()
    {
        Dictionary<int3, IChunk> result = new Dictionary<int3, IChunk>();
        for (int i = 0; i < chunks.Length / 3; i++)
        {
            int3 a = new int3(
                    chunks[i, 0], 
                    chunks[i, 1], 
                    chunks[i, 2]);
            result.Add(a, null);
            
        }
        return result;
    }
}