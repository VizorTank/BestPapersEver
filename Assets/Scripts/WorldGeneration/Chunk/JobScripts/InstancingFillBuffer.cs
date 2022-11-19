using Unity.Collections;
using Unity.Jobs;

public struct InstancingFillBuffer : IJob
{
    [ReadOnly] public NativeArray<int> BlockIds;
    public UnityEngine.ComputeBuffer Buffer;
    public void Execute()
    {
        Buffer.SetData(BlockIds);
    }
}