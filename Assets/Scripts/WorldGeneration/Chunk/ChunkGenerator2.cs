using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class ChunkGenerator2
{
    private IChunk _chunk;
    private IWorld _world;
    private JobHandle _generatingBlockIdJobHandle;
    private ChunkGeneraionBiomes _chunkGeneraionBiomes;
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
            // Debug.Log("Created" + blocksId.Length);
            generatedBlocks = new NativeArray<int>(blocksId, Allocator.Persistent);
            blocksId.Dispose();

            CreateTrees();
            // Debug.Log("Finished Generating");
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

        int3 chunkPos = _chunk.GetChunkCoordinates();
        // _chunkGeneraionBiomes = _world.GetChunkGeneraionBiomes(chunkPos);

        GenerateBlockIdJob generateBlockIdJob = new GenerateBlockIdJob
        {
            blockIdDatas = blocksId,

            // center = _chunk.GetBiome(),
            // back = _world.GetBiome(chunkPos + VoxelData.voxelNeighbours[0]),
            // front = _world.GetBiome(chunkPos + VoxelData.voxelNeighbours[1]),
            // left = _world.GetBiome(chunkPos + VoxelData.voxelNeighbours[4]),
            // right = _world.GetBiome(chunkPos + VoxelData.voxelNeighbours[5]),

            // Lodes = _world.GetLodes(chunkPos),

            chunkSize = VoxelData.ChunkSize,
            chunkPosition = new int3(_chunk.GetChunkPosition()),

            // ChunkGeneraionBiomes = _chunkGeneraionBiomes
            ChunkGeneraionBiomes = _world.GetChunkGeneraionBiomes(chunkPos),
            HeightMap = _world.GetHeightMap(chunkPos)
        };

        _generatingBlockIdJobHandle = generateBlockIdJob.Schedule(size, 32); 
    }

    private void CreateTrees()
    {
        // BiomeAttributesStruct biome = _chunk.GetBiome();
        float3 chunkPos = _chunk.GetChunkPosition();
        BiomeAttributesStruct biome = _chunk.GetBiome();
        if (chunkPos.y + VoxelData.ChunkSize.y < biome.solidGroundHeight) return;

        int treeCount = (int)(math.pow(0.5 + noise.snoise(new float2(chunkPos.x, chunkPos.z)) / 2, 1 / biome.treeThreshold) * biome.treeMaxCount);

        if (treeCount <= 0) return;
        
        var generationBiome = _world.GetChunkGeneraionBiomes(_chunk.GetChunkCoordinates());
        // treeCount = 1;
        for (int i = 0; i < treeCount; i++)
        {
            int x = (int)((0.5 + noise.snoise(new float2(chunkPos.x + i, chunkPos.z)) / 2) * VoxelData.ChunkSize.x) + (int)chunkPos.x;
            // int x = (int)(0.5 * VoxelData.ChunkSize.x) + (int)chunkPos.x;
            int z = (int)((0.5 + noise.snoise(new float2(chunkPos.x, chunkPos.z + i)) / 2) * VoxelData.ChunkSize.z) + (int)chunkPos.z;
            // int z = (int)(0.5 * VoxelData.ChunkSize.z) + (int)chunkPos.z;
            int y = generationBiome.CalculateTerrainHeight(new int3(x, 0, z));
            // if (math.any(_chunk.GetChunkCoordinates() < 0))
            //     Debug.Log($"Chunk Pos: {chunkPos.x}, {chunkPos.y}, {chunkPos.z} Tree Pos: {x}, {z}, {y}");
            // int y = biome.CalculateTerrainHeight(new float3(x, 0, z));
            if (y <= generationBiome.WaterLevel) continue;
            _world.CreateStructure(new int3(x - 2, y + 1, z - 2), 0);
            // Debug.Log("Created tree");
        }
        // generationBiome.Destroy();
    }

    private bool FinishJob()
    {
        if (State != ChunkGeneratorStates.CreatedJob || !_generatingBlockIdJobHandle.IsCompleted) return false;
        State = ChunkGeneratorStates.Ready;
        _generatingBlockIdJobHandle.Complete();
        // _chunkGeneraionBiomes.Destroy();
        return true;
    }
}

public enum ChunkGeneratorStates
{
    Ready,
    CreatedJob
}