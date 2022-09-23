using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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

    public ChunkData(int3 chunkCoordinates)
    {
        Coords = new [] {chunkCoordinates.x, chunkCoordinates.y, chunkCoordinates.z};
    }

    public override string ToString()
    {
        return "chunk" 
            + "_" + Coords[0]
            + "_" + Coords[1]
            + "_" + Coords[2];
    }
}
