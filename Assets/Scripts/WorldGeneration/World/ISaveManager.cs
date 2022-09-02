using Unity.Mathematics;

public interface ISaveManager
{
    public IChunk LoadChunk(IWorld world, int3 chunkCoordinates);
    public void SetChunkActive(int3 chunkCoordinates);

    public void RemoveUnusedChunks();
}