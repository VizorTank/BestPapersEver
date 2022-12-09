using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class ChunkGenerator2
{
    private IChunk _chunk;
    private IWorld _world;
    private JobHandle _generatingBlockIdJobHandle;
    public ChunkGeneratorStates State = ChunkGeneratorStates.Ready;

    private NativeArray<int> blocksId;

    public void Init(IChunk chunk, IWorld world)
    {
        if (_chunk != null) return;
        _chunk = chunk;
        _world = world;
    }

    public bool GenerateBlocks(IChunk chunk, IWorld world, out NativeArray<int> generatedBlocks)
    {
        Init(chunk, world);

        CreateJob();
        if (FinishJob())
        {
            generatedBlocks = blocksId;
            return true;
        }

        generatedBlocks = new NativeArray<int>();
        return false;
    }

    private void CreateJob()
    {
        if (State != ChunkGeneratorStates.Ready) return;
        State = ChunkGeneratorStates.CreatedJob;

        int size = VoxelData.ChunkSize.x * VoxelData.ChunkSize.y * VoxelData.ChunkSize.z;
        blocksId = new NativeArray<int>(size, Allocator.Persistent);

        int3 chunkPos = _chunk.GetChunkCoordinates();

        GenerateBlockIdJob generateBlockIdJob = new GenerateBlockIdJob
        {
            blockIdDatas = blocksId,

            chunkSize = VoxelData.ChunkSize,
            chunkPosition = new int3(_chunk.GetChunkPosition()),

            ChunkGeneraionBiomes = _world.GetChunkGeneraionBiomes(chunkPos),
            HeightMap = _world.GetHeightMap(chunkPos)
        };

        _generatingBlockIdJobHandle = generateBlockIdJob.Schedule(size, 32); 
    }

    private bool FinishJob()
    {
        if (State != ChunkGeneratorStates.CreatedJob || !_generatingBlockIdJobHandle.IsCompleted) return false;
        State = ChunkGeneratorStates.Ready;
        _generatingBlockIdJobHandle.Complete();
        return true;
    }
}

public enum ChunkGeneratorStates
{
    Ready,
    CreatedJob
}