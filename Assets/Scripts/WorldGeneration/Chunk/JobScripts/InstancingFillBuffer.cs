using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct InstancingFillBuffer : IJobParallelFor
{
    public NativeArray<int> BlockIds;
    public void Execute(int index)
    {
        int3 pos = VoxelData.GetPosition(index);
        BlockIds[index] = (pos.x + pos.y + pos.z) % 2;
        BlockIds[index] = 1;
    }
}