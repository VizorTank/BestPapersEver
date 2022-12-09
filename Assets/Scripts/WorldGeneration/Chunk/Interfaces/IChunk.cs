using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public interface IChunk
{
    public void Init();
    public void Init(ChunkData data);
    public void Destroy();
    public bool IsDestroyed();
    public int3 GetChunkCoordinates();
    public float3 GetChunkPosition();
    public void Render();
    public void Update();
    public void UpdateNeighbours();
    public void UpdateListOfNeighbours();
    public bool TryGetBlock(int3 position, out int blockId);
    public bool TrySetBlock(int3 position, int placedBlockId, out int replacedBlockId);
    public NativeArray<int> GetBlocks();
    public NativeArray<int> GetSharedData();
    public ComputeBuffer GetBlocksBuffer();
    public void ReleaseSharedData();
    public bool CanAccess();
    public bool IsCreated();
    public bool CanBeSaved();
    public void CreateStructure(int3 structurePosition, int structureId);
}