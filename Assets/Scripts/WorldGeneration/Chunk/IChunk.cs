using Unity.Collections;
using Unity.Mathematics;
public interface IChunk
{
    public void Init();
    public void Init(ChunkData data);
    public void Destroy();
    public bool IsDestroyed();
    public int GetBiomeIndex();
    public int3 GetChunkCoordinates();
    public float3 GetChunkPosition();
    public BiomeAttributesStruct GetBiome();
    public void Render();
    public void Update();
    public void UpdateData();
    public void UpdateNeighbours();
    public void UpdateNeighbourList();
    public void Hide();
    public bool TryGetBlock(int3 position, out int blockId);
    public bool TrySetBlock(int3 position, int placedBlockId, out int replacedBlockId);
    public NativeArray<int> GetBlocks();
    public NativeArray<int> GetSharedData();
    public void ReleaseSharedData();
    public bool CanAccess();
    public bool CanBeSaved();
    public void CreateStructure(int3 structurePosition, int structureId);
}