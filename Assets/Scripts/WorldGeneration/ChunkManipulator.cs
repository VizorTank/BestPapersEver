

using Unity.Collections;
using Unity.Mathematics;

public class ChunkManipulator
{
    private IChunk _chunk;
    private WorldClass _world;
    private NativeArray<int> _blocksId;

    private int _sharedCount;
    private NativeArray<int> _sharedData;

    private ChunkGenerator2 _chunkGenerator;

    public ChunkManipulator(IChunk chunk, WorldClass world)
    {
        _chunk = chunk;
        _world = world;

        _chunkGenerator = new ChunkGenerator2();
    }

    public void Destroy()
    {
        _blocksId.Dispose();
    }

    public void GenerateBlocks()
    {
        if (CanAccess()) return;
        if (_chunkGenerator.GenerateBlocks(_chunk, _world, out NativeArray<int> generatedBlocks))
        {
            _blocksId = generatedBlocks;
            _chunk.Update();
        }
    }

    public bool CanAccess()
    {
        return _blocksId != null && _blocksId.Length != 0;
    }

    #region GetDataForNeighbours

    public NativeArray<int> GetSharedData()
    {
        if (_sharedCount <= 0)
        {
            _sharedData = new NativeArray<int>(_blocksId, Allocator.Persistent);
            _sharedCount = 0;
        }
        _sharedCount++;
        return _sharedData;
    }

    public void ReleaseSharedData()
    {
        _sharedCount--;
        if (_sharedCount <= 0)
        {
            _sharedData.Dispose();
        }
    }

    #endregion

    public void PlaceStructure()
    {

    }

    #region AccessChunk

    public int GetBlock(int3 blockPosition)
    {
        return _blocksId[VoxelData.GetIndex(blockPosition)];
    }

    public NativeArray<int> GetBlocks()
    {
        return _blocksId;
    }

    public void SetBlock(int3 blockPosition, int blockId)
    {
        _blocksId[VoxelData.GetIndex(blockPosition)] = blockId;
        _chunk.Update();
    }
        
    #endregion

    #region Save&Load

    public void Load()
    {

    }

    public void Save()
    {

    }

    #endregion
}