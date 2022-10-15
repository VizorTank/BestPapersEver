using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GenerateBlockIdJob : IJobParallelFor
{
    public NativeArray<int> blockIdDatas;

    [ReadOnly] public int3 chunkSize;
    [ReadOnly] public int3 chunkPosition;

    // [ReadOnly] public BiomeAttributesStruct center;
    // [ReadOnly] public BiomeAttributesStruct back;
    // [ReadOnly] public BiomeAttributesStruct front;
    // [ReadOnly] public BiomeAttributesStruct left;
    // [ReadOnly] public BiomeAttributesStruct right;

    // [ReadOnly] public NativeArray<LodeStruct> Lodes;

    [ReadOnly] public ChunkGeneraionBiomes ChunkGeneraionBiomes;
    [ReadOnly] public NativeArray<int> HeightMap;

    public void Execute(int index)
    {
        int x = index % chunkSize.x;
        int y = ((index - x) / chunkSize.x) % chunkSize.y;
        int z = (((index - x) / chunkSize.x) - y) / chunkSize.y;
        int3 position = new int3(x, y, z) + chunkPosition;

        // int terrainHeight = ChunkGeneraionBiomes.CalculateTerrainHeight(new int3(position));

        // blockIdDatas[index] = ChunkGeneraionBiomes.Biomes[ChunkGeneraionBiomes.Center].GetBlock(
        //     position, 
        //     terrainHeight, 
        //     ChunkGeneraionBiomes.Lodes,
        //     ChunkGeneraionBiomes.WaterLevel);
        // blockIdDatas[index] = ChunkGeneraionBiomes.GetBlock(position);
        blockIdDatas[index] = ChunkGeneraionBiomes.GetBlock(position, HeightMap[x + z * chunkSize.x]);
    }
}