using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class ChunkGenerator2
{
    private WorldClass _world;
    private IChunk _chunk;
    private JobHandle _generatingBlockIdJobHandle;
    public ChunkGeneratorStates State = ChunkGeneratorStates.Ready;

    private NativeArray<int> blocksId;

    public void Init(IChunk chunk, WorldClass world)
    {
        if (_chunk != null) return;
        _chunk = chunk;
        _world = world;
    }

    public bool GenerateBlocks(IChunk chunk, WorldClass world, out NativeArray<int> generatedBlocks)
    {
        Init(chunk, world);
        CreateJob();
        if (FinishJob())
        {
            // Debug.Log("Created" + blocksId.Length);
            generatedBlocks = new NativeArray<int>(blocksId, Allocator.Persistent);
            blocksId.Dispose();
            return true;
        }

        generatedBlocks = new NativeArray<int>();
        return false;
    }

    private void CreateJob()
    {
        if (State != ChunkGeneratorStates.Ready) return;
        State = ChunkGeneratorStates.CreatedJob;
        // Debug.Log("Created");

        int size = VoxelData.ChunkSize.x * VoxelData.ChunkSize.y * VoxelData.ChunkSize.z;
        blocksId = new NativeArray<int>(size, Allocator.Persistent);

        GenerateBlockIdJob generateBlockIdJob = new GenerateBlockIdJob
        {
            blockIdDatas = blocksId,

            biome = _chunk.GetBiome(),

            chunkSize = VoxelData.ChunkSize,
            chunkPosition = _chunk.GetChunkPosition()
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