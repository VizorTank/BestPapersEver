using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GenerateBlockIdJob : IJobParallelFor
{
    public NativeArray<int> blockIdDatas;

    [ReadOnly] public int3 chunkSize;
    [ReadOnly] public float3 chunkPosition;

    [ReadOnly] public BiomeAttributesStruct biome;
    public void Execute(int index)
    {
        int x = index % chunkSize.x;
        int y = ((index - x) / chunkSize.x) % chunkSize.y;
        int z = (((index - x) / chunkSize.x) - y) / chunkSize.y;
        float3 position = new float3(x, y, z) + chunkPosition;

        blockIdDatas[index] = biome[position];
    }
}