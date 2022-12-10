using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public interface IWorld
{
    public string GetWorldName();
    public void SetRenderDistance(int renderDistance);
    public int GetRenderDistance();
    public void SetRenderType(RenderType type);

    public int GetWorldHeightInChunks();

    public float GetEnemiesDensity();

    public bool TryGetBlock(float3 position, ref int blockID);
    public bool TrySetBlock(Vector3 position, int blockID, ref int replacedBlockId);

    public WorldBiomesList GetWorldBiomesList();
    public BiomeAttributesStruct GetBiome(int index);
    public ChunkGeneraionBiomes GetChunkGeneraionBiomes(int3 ChunkCoord);
    public NativeArray<int> GetHeightMap(int3 ChunkCoord);
    public IChunk GetChunk(int3 chunkCoordinates);
    public IChunk GetOrCreateChunk(int3 coordinates);
    public Transform GetTransform();
    public Vector3 GetPlayerPosition();
    public BlockTypesList GetBlockTypesList();

    public Structure GetStructure(int structureId);
    public void CreateStructure(int3 position, int structureId);
    public void CreateStructure(int3 chunkPos, int3 structurePos, int structureId);
    public RenderType GetRenderType();
    public void RemoveChunk(int3 chunkCoordinates);
}