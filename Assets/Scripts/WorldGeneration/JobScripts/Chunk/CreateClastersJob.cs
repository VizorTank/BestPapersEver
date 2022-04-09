using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CreateClastersJob : IJob
{
    // Input
    [ReadOnly] public NativeArray<int> blockIdDatas;

    // Output
    public NativeArray<int> blocksClusterIdDatas;
    public NativeList<int> clusterBlockIdDatas;
    public NativeList<int3> clusterSizeDatas;
    public NativeList<int3> clusterPositionDatas;
    public NativeList<ClusterSidesVisibility> clusterSidesVisibilityData;

    // Const
    [ReadOnly] public NativeArray<int3> axis;
    [ReadOnly] public int3 chunkSize;
    public void Execute()
    {
        NativeArray<bool> blockChecked = new NativeArray<bool>(blockIdDatas.Length, Allocator.Temp);
        for (int i = 0; i < blockChecked.Length; i++)
        {
            blockChecked[i] = false;
        }

        int clusterSizeDatasIndex = 0;

        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                for (int z = 0; z < chunkSize.z; z++)
                {
                    int idx = x + (y + z * chunkSize.y) * chunkSize.x;
                    if (blockChecked[idx] || blockIdDatas[idx] == 0) continue;

                    // Unnessesary
                    blockChecked[idx] = true;

                    int blockId = blockIdDatas[idx];
                    int clusterId = clusterSizeDatasIndex;

                    NativeArray<int> clusterSize = new NativeArray<int>(3, Allocator.Temp);
                    clusterSize[0] = 1;
                    clusterSize[1] = 1;
                    clusterSize[2] = 1;

                    for (int axisIndex = 0; axisIndex < 3; axisIndex++)
                    {
                        int nextCount = 1;
                        while (true)
                        {
                            int3 shift = axis[axisIndex] * nextCount;
                            // Check if outside of chunk
                            if (shift.x >= chunkSize.x - x ||
                                shift.y >= chunkSize.y - y ||
                                shift.z >= chunkSize.z - z) break;

                            bool flag = true;
                            int blocksToAddIndex = 0;
                            NativeArray<int> blocksToAdd = new NativeArray<int>(clusterSize[0] * clusterSize[1], Allocator.Temp);
                            // Check block/Line/Rectangle
                            // Z
                            for (int i = 0; i < clusterSize[0]; i++)
                            {
                                // Y
                                for (int j = 0; j < clusterSize[1]; j++)
                                {
                                    int3 pos = new int3(x + shift.x, y + j + shift.y, z + i + shift.z);
                                    int index = pos.x + (pos.y + pos.z * chunkSize.y) * chunkSize.x;
                                    if (blockIdDatas[index] != blockId || blockChecked[index])
                                    {
                                        flag = false;
                                        break;
                                    }
                                    blocksToAdd[blocksToAddIndex++] = index;
                                }
                                if (!flag) break;
                            }
                            if (!flag) break;

                            nextCount++;
                            // Add block to cluster
                            foreach (int blockIndex in blocksToAdd)
                            {
                                blocksClusterIdDatas[blockIndex] = clusterId;
                                blockChecked[blockIndex] = true;
                            }
                            blocksToAdd.Dispose();
                        }
                        clusterSize[axisIndex] = nextCount;
                    }
                    clusterSizeDatas.Add(new int3(clusterSize[2], clusterSize[1], clusterSize[0]));
                    clusterPositionDatas.Add(new int3(x, y, z));
                    clusterBlockIdDatas.Add(blockId);
                    clusterSizeDatasIndex = clusterSizeDatas.Length;
                    ClusterSidesVisibility clusterSidesVisibility = new ClusterSidesVisibility();
                    for (int i = 0; i < 6; i++)
                    {
                        clusterSidesVisibility[i] = 0;
                    }
                    clusterSidesVisibilityData.Add(clusterSidesVisibility);

                    clusterSize.Dispose();
                }
            }
        }
        blockChecked.Dispose();
    }
}