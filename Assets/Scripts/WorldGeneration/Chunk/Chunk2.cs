using Unity.Collections;
using Unity.Mathematics;

public class Chunk : IChunk
{
    private WorldClass _world;

    private ChunkRenderer _renderer;
    private ChunkManipulator _manipulator;

    private int3 _chunkCoordinates;

    public Chunk(WorldClass world, int3 chunkCoordinates)
    {
        _world = world;
        _chunkCoordinates = chunkCoordinates;

        _renderer = new ChunkRenderer(this, _world);
        _manipulator = new ChunkManipulator(this, world);
    }

    public Chunk(IWorld world, int3 chunkCoordinates)
    {
        // _world = world;
        // _chunkCoordinates = chunkCoordinates;

        // _renderer = new ChunkRenderer(this, _world);
        // _manipulator = new ChunkManipulator(this, world);
    }
    
    public void Destroy()
    {
        _renderer.Destroy();
        _manipulator.Destroy();
    }

    public BiomeAttributesStruct GetBiome()
    {
        return _world.GetBiome(GetChunkCoordinates());
    }

    public NativeArray<int> GetBlocks()
    {
        return _manipulator.GetBlocks();
    }

    public int3 GetChunkCoordinates()
    {
        return _chunkCoordinates;
    }

    private bool dataExists = false;
    private ChunkNeighbourData data;

    public ChunkNeighbourData GetNeighbourData()
    {
        if (dataExists) return data;
        ChunkNeighbours neighbours = _world.GetNeighbours(GetChunkCoordinates());

        data = new ChunkNeighbourData();

        for (int i = 0; i < 6; i++)
        {
            if (neighbours[i] == null)
            {
                data.ChunkNeighbourDataArray[i] = new NativeArray<int>(0, Allocator.Persistent);
                data.ChunkNeighbourDataValid[i] = false;
            }
            else
            {
                data.ChunkNeighbourDataArray[i] = neighbours[i].GetSharedData();
                data.ChunkNeighbourDataValid[i] = true;
            }
        }
        dataExists = true;
        return data;
    }

    public void ReleaseNeighbourData()
    {
        if (!dataExists) return;

        ChunkNeighbours neighbours = _world.GetNeighbours(GetChunkCoordinates());
        for (int i = 0; i < 6; i++)
        {
            if (data.ChunkNeighbourDataValid[i])
                neighbours[i].ReleaseSharedData();
            else
                data.ChunkNeighbourDataArray[i].Dispose();
        }
        dataExists = false;
    }

    public NativeArray<int> GetSharedData()
    {
        return _manipulator.GetSharedData();
    }

    public void ReleaseSharedData()
    {
        _manipulator.ReleaseSharedData();
    }

    public void Render()
    {
        _manipulator.GenerateBlocks();
        _renderer.Render();
    }

    public void Save()
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetBlock(int3 position, out int blockId)
    {
        blockId = 0;
        if (!_manipulator.CanAccess()) return false;
        blockId = _manipulator.GetBlock(position);
        return true;
    }

    public bool TrySetBlock(int3 position, int placedBlockId, out int replacedBlockId)
    {
        replacedBlockId = 0;
        if (!_manipulator.CanAccess()) return false;
        replacedBlockId = _manipulator.GetBlock(position);
        _manipulator.SetBlock(position, placedBlockId);
        return true;
    }

    public float3 GetChunkPosition()
    {
        return new float3(GetChunkCoordinates()) * VoxelData.ChunkSize;
    }

    public void Update()
    {
        _renderer.Update();
    }

    public void Hide()
    {
        throw new System.NotImplementedException();
    }
}