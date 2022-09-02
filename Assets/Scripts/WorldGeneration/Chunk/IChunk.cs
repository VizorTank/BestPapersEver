using Unity.Collections;
using Unity.Mathematics;
public interface IChunk
{
    public void Destroy();
    public int3 GetChunkCoordinates();
    public float3 GetChunkPosition();
    public BiomeAttributesStruct GetBiome();
    public void Render();
    public void Update();
    public void Hide();
    public bool TryGetBlock(int3 position, out int blockId);
    public bool TrySetBlock(int3 position, int placedBlockId, out int replacedBlockId);
    public NativeArray<int> GetBlocks();
    public ChunkNeighbourData GetNeighbourData();
    public void ReleaseNeighbourData();
    public NativeArray<int> GetSharedData();
    public void ReleaseSharedData();
    public void Save();
}