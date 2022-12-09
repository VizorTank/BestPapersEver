using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ChunkColumnData
{
    private IWorld _world;
    public int BiomeIndex;
    public ChunkGeneraionBiomes ChunkGeneraionBiomes;
    public int2 ColumnCoordinates;
    public NativeArray<int> HeightMap;

    public ChunkColumnData(IWorld world, int3 coordinates)
    {
        _world = world;
        ColumnCoordinates = new int2(coordinates.x, coordinates.z);

        HeightMap = new NativeArray<int>(VoxelData.ChunkSize.x * VoxelData.ChunkSize.y * VoxelData.ChunkSize.z, Allocator.Persistent);
        GenerateBiomeIndex();
        GenerateHeightMap();
        GenerateTreesPosition();
    }

    public void Destroy()
    {
        try { HeightMap.Dispose(); } catch { }
    }

    public void GenerateBiomeIndex()
    {
        WorldBiomesList list = _world.GetWorldBiomesList();
        ChunkGeneraionBiomes = list.GetChunkGeneraionBiomes(_world, new int3(ColumnCoordinates.x, 0, ColumnCoordinates.y));
        BiomeIndex = ChunkGeneraionBiomes.Center;
    }

    public void GenerateHeightMap()
    {
        int3 chunkPos = new int3(ColumnCoordinates.x, 0, ColumnCoordinates.y) * VoxelData.ChunkSize;
        for (int x = 0; x < VoxelData.ChunkSize.x; x++)
        {
            for (int z = 0; z < VoxelData.ChunkSize.z; z++)
            {
                HeightMap[x + z * VoxelData.ChunkSize.x] = ChunkGeneraionBiomes.CalculateTerrainHeight(new int3(x, 0, z) + chunkPos);
            }
        }
    }

    public int GetTerrainHeight(int3 position) => HeightMap[GetIndex(position)];

    public int GetIndex(int3 position)
    {
        int3 chunkPos = new int3(ColumnCoordinates.x, 0, ColumnCoordinates.y) * VoxelData.ChunkSize;
        position -= chunkPos;
        return position.x + position.z * VoxelData.ChunkSize.x;
    }

    public void GenerateTreesPosition()
    {
        float2 chunkPos = ColumnCoordinates * VoxelData.ChunkSideSize;
        BiomeAttributesStruct biome = _world.GetBiome(BiomeIndex);

        int treeCount = (int)(math.pow(0.5 + noise.snoise(chunkPos) / 2, 1 / biome.treeThreshold) * biome.treeMaxCount);

        if (treeCount <= 0) return;
        
        var generationBiome = ChunkGeneraionBiomes;
        for (int i = 0; i < treeCount; i++)
        {
            int x = (int)((0.5 + noise.snoise(new float2(chunkPos.x + i, chunkPos.y)) / 2) * VoxelData.ChunkSize.x) + (int)chunkPos.x;
            int z = (int)((0.5 + noise.snoise(new float2(chunkPos.x, chunkPos.y + i)) / 2) * VoxelData.ChunkSize.z) + (int)chunkPos.y;
            int y = GetTerrainHeight(new int3(x, 0, z));
            if (y <= generationBiome.WaterLevel) continue;
            _world.CreateStructure(new int3(x - 2, y + 1, z - 2), 0);
        }
    }
}
