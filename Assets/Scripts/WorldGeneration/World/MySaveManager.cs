using System.Collections.Generic;
using Unity.Mathematics;

public class MySaveManager : ISaveManager
{
    #region Singleton
    private static MySaveManager _instance;
    private MySaveManager() { }
    public static ISaveManager GetInstance()
    {
        if (_instance == null)
            _instance = new MySaveManager();
        
        return _instance;
    }
    #endregion

    private Dictionary<int3, IChunk> _createdChunks;

    public IChunk LoadChunk(IWorld world, int3 chunkCoordinates)
    {
        if (_createdChunks.ContainsKey(chunkCoordinates))
            _createdChunks.Add(chunkCoordinates, new Chunk(world, chunkCoordinates));
        return _createdChunks[chunkCoordinates];
    }

    public void SetChunkActive(int3 chunkCoordinates)
    {
        throw new System.NotImplementedException();
    }

    public void RemoveUnusedChunks()
    {
        throw new System.NotImplementedException();
    }
}