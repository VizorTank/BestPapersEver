using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData
{
    public int[] Coords;
    public int[] BlockIds;
    
    public ChunkData(IChunk chunk)
    {
        Coords = new [] {chunk.GetChunkCoordinates().x, chunk.GetChunkCoordinates().y, chunk.GetChunkCoordinates().z};
        BlockIds = chunk.GetBlocks().ToArray();
    }
}
