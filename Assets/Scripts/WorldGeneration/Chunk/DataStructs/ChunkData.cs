using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData
{
    public int[] Coords;
    public int[] BlockIds;
    
    public ChunkData(Chunk chunk)
    {
        Coords = new [] {chunk.coordinates.x, chunk.coordinates.y, chunk.coordinates.z};
        BlockIds = chunk.GetBlocks().ToArray();
    }
}
