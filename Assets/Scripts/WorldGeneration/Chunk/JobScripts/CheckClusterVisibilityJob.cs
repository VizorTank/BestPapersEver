using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CheckClusterVisibilityJob : IJobParallelFor
{
    // Input
    [ReadOnly] public NativeArray<int> blockIdDatas;
    [ReadOnly] public ChunkNeighbourData chunkNeighbourData;

    // Output
    public NativeArray<ClusterCreationStruct> ClusterData;
    public NativeList<ClusterSidesDataStruct>.ParallelWriter Writer;

    // Const
    [ReadOnly] public NativeArray<int3> neighbours;
    [ReadOnly] public NativeArray<int3> clusterSides;
    [ReadOnly] public int3 chunkSize;
    [ReadOnly] public NativeArray<bool> blockTypesIsTransparent;

    public void Execute(int index)
    {
        var cluster = ClusterData[index];
        var clusterSize = cluster.Size;
        var blockId = cluster.BlockId;

        NativeArray<int3> axis = new NativeArray<int3>(6, Allocator.Temp);
        axis[0] = neighbours[0];
        axis[1] = neighbours[1] * clusterSize.z;
        axis[2] = neighbours[2] * clusterSize.y;
        axis[3] = neighbours[3];
        axis[4] = neighbours[4];
        axis[5] = neighbours[5] * clusterSize.x;

        NativeArray<int3> clusterSideSizes = new NativeArray<int3>(6, Allocator.Temp);
        clusterSideSizes[0] = clusterSides[0] * clusterSize + new int3(0, 0, 1);
        clusterSideSizes[1] = clusterSides[1] * clusterSize + new int3(0, 0, 1);
        clusterSideSizes[2] = clusterSides[2] * clusterSize + new int3(0, 1, 0);
        clusterSideSizes[3] = clusterSides[3] * clusterSize + new int3(0, 1, 0);
        clusterSideSizes[4] = clusterSides[4] * clusterSize + new int3(1, 0, 0);
        clusterSideSizes[5] = clusterSides[5] * clusterSize + new int3(1, 0, 0);

        ClusterSidesVisibility clusterSidesVisibility = new ClusterSidesVisibility();

        for (int i = 0; i < 6; i++)
        {
            int3 startPosition = cluster.Position + axis[i];
            bool isVisible = false;
            for (int x = 0; x < clusterSideSizes[i].x; x++)
            {
                for (int y = 0; y < clusterSideSizes[i].y; y++)
                {
                    for (int z = 0; z < clusterSideSizes[i].z; z++)
                    {
                        int neighbourBlockId;
                        if (startPosition.x < 0 || startPosition.x >= chunkSize.x ||
                            startPosition.y < 0 || startPosition.y >= chunkSize.y ||
                            startPosition.z < 0 || startPosition.z >= chunkSize.z)
                        {
                            // if (!chunkNeighbourData.ChunkNeighbourDataValid[i]) continue;
                            int3 blockPos = (startPosition % chunkSize + chunkSize) % chunkSize + new int3(x, y, z);
                            int idx = blockPos.x + (blockPos.y + blockPos.z * chunkSize.y) * chunkSize.x;

                            neighbourBlockId = chunkNeighbourData.ChunkNeighbourDataArray[i][idx];
                        }
                        else
                        {
                            int3 blockPos = startPosition + new int3(x, y, z);
                            int idx = blockPos.x + (blockPos.y + blockPos.z * chunkSize.y) * chunkSize.x;

                            neighbourBlockId = blockIdDatas[idx];
                        }
                        // Add when block hides side
                        if (!blockTypesIsTransparent[neighbourBlockId] || blockId == neighbourBlockId)
                            clusterSidesVisibility[i]++;
                        else
                            isVisible = true;
                    }
                }
            }
            var data = new ClusterSidesDataStruct {
                    BlockId = cluster.BlockId,
                    Position = cluster.Position,
                    Rotation = i,
                    Size = cluster.Size,
                    Visibility = clusterSidesVisibility[i]
            };
            if (isVisible)
                Writer.AddNoResize(data);
        }
        
        cluster.Visibility = clusterSidesVisibility;
        ClusterData[index] = cluster;
    }
}
