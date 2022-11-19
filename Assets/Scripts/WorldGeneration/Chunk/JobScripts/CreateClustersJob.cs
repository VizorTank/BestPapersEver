using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CreateClustersJob : IJob
{
    // Input
    [ReadOnly] public NativeArray<int> blockIdDatas;

    // Output
    public NativeArray<int> blocksClusterIdDatas;
    public NativeList<ClusterCreationStruct> Clusters;

    // Const
    [ReadOnly] public NativeArray<int3> axis;
    [ReadOnly] public int3 chunkSize;
    
    public void Execute()
    {
        NativeArray<bool> blockChecked = new NativeArray<bool>(blockIdDatas.Length, Allocator.Temp);
        NativeArray<int> clusterSize = new NativeArray<int>(3, Allocator.Temp);

        int clusterSizeDatasIndex = 0;
        // For each block
        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                for (int z = 0; z < chunkSize.z; z++)
                {
                    int idx = x + (y + z * chunkSize.y) * chunkSize.x;
                    // Check if block belongs to cluster
                    if (blockChecked[idx] || blockIdDatas[idx] == 0) continue;

                    int blockId = blockIdDatas[idx];
                    int clusterId = clusterSizeDatasIndex;
                    
                    clusterSize[0] = 1;
                    clusterSize[1] = 1;
                    clusterSize[2] = 1;

                    // For each axis (Z => Y => X)
                    for (int axisIndex = 0; axisIndex < 3; axisIndex++)
                    {
                        // Length of cluster in axisIndex axis
                        int nextCount = 1;

                        // While cluster can be bigger
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
                            // Check if can add to cluster block/Line/Rectangle
                            // Z (Equals to 1 for checking Z axis)
                            for (int i = 0; i < clusterSize[0]; i++)
                            {
                                // Y (Equals to 1 for checking Z and Y axis)
                                for (int j = 0; j < clusterSize[1]; j++)
                                {
                                    int3 pos = new int3(x + shift.x, y + j + shift.y, z + i + shift.z);
                                    int index = pos.x + (pos.y + pos.z * chunkSize.y) * chunkSize.x;
                                    // Check if block belongs to another cluster or is different type
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

                            // Increase length of cluster in axisIndex axis
                            nextCount++;
                            // Add block to cluster
                            foreach (int blockIndex in blocksToAdd)
                            {
                                blocksClusterIdDatas[blockIndex] = clusterId;
                                blockChecked[blockIndex] = true;
                            }
                            blocksToAdd.Dispose();
                        }
                        // Set cluster size in axisIndex axis
                        clusterSize[axisIndex] = nextCount;
                    }
                    
                    // Create and add cluster
                    var cluster = new ClusterCreationStruct()
                    {
                        BlockId = blockId,
                        Position = new int3(x, y, z),
                        Size = new int3(clusterSize[2], clusterSize[1], clusterSize[0]),
                        Visibility = new ClusterSidesVisibility()
                    };
                    Clusters.Add(cluster);
                    clusterSizeDatasIndex = Clusters.Length;
                }
            }
        }
        clusterSize.Dispose();
        blockChecked.Dispose();
    }
}