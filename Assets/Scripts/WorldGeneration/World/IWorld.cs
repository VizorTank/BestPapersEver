using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public interface IWorld
{
    // public void DrawChunks();
    // public void CreateNewChunk(int3 chunkCoordinates);
    // public void SetChunkActive(int3 chunkCoordinates);
    // public bool IsInWorld(int3 position);
    public bool TryGetBlock(float3 position, ref int blockID);
    public bool TryPlaceBlock(Vector3 position, int blockID, ref int replacedBlockId);
    public bool TryGetBlocks(Vector3 position, NativeArray<int> blockIds);
    public bool TrySetBlock(Vector3 position, int blockID, ref int replacedBlockId);

    public BiomeAttributesStruct GetBiome(int3 chunkCoordinates);
    public BiomeAttributesStruct GetBiome(int index);
    public int GetBiomeIndex(int3 chunkCoordinates);
    public NativeArray<LodeStruct> GetLodes(int3 ChunkCoord);
    public ChunkGeneraionBiomes GetChunkGeneraionBiomes(int3 ChunkCoord);
    public IChunk GetChunk(int3 chunkCoordinates);
    public IChunk GetOrCreateChunk(int3 coordinates);
    // public ChunkNeighbours GetNeighbours(int3 chunkCoordinates);
    public bool TryGetNeighbours(int3 chunkCoordinates, ref ChunkNeighbours neighbours);
    public Transform GetTransform();
    public BlockTypesList GetBlockTypesList();

    public Structure GetStructure(int structureId);
    public void CreateStructure(Vector3 position, int structureId);
    public void CreateStructure(int3 position, int structureId);
    public void CreateStructure(int3 chunkPos, int3 structurePos, int structureId);
    // public void CreateStructure(int3 position, Structure structure);
}