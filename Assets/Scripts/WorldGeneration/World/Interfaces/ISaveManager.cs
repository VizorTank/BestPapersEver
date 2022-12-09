using Unity.Mathematics;
using System.Collections.Generic;

public interface ISaveManager
{
    public void SaveWorld(IWorld world, Dictionary<int3, IChunk> chunksToUnload);
    public void SaveWorld();
    public void LoadWorld();

    public void Run();
    public void LoadingChunks();

    public IChunk LoadChunk(IWorld world, int3 chunkCoordinates);
    public void UnloadChunks(IWorld world, Dictionary<int3, IChunk> chunksToUnload);
    public void UnloadChunk(IWorld world, IChunk chunk);
}