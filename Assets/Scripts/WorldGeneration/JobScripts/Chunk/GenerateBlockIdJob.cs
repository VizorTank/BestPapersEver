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
    public void Execute(int index)
    {
        int x = index % chunkSize.x;
        int y = ((index - x) / chunkSize.x) % chunkSize.y;
        int z = (((index - x) / chunkSize.x) - y) / chunkSize.y;
        float3 position = new float3(x, y, z) + chunkPosition;
        float scale = 0.2f;
        float offset = 0;
        int terrainHeightDifference = 4;
        int terrainSolidGround = 12;

        int yPos = (int)math.floor(position.y);

        byte voxelValue = 0;

        int terrainHeight = (int)math.floor((noise.snoise(
            new float2(
                position.x / chunkSize.x * scale + offset,
                position.z / chunkSize.z * scale + offset)) + 1.0) / 2 * terrainHeightDifference) + terrainSolidGround;

        if (yPos > terrainHeight)
            voxelValue = 0;
        else if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos > terrainHeight - 4)
            voxelValue = 4;
        else if (yPos < terrainHeight)
            voxelValue = 2;

        blockIdDatas[index] = voxelValue;
    }
}