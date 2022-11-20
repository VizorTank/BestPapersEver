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
    [ReadOnly] public NativeArray<ClusterCreationStruct> ClusterData;

    // Output
    public NativeList<ClusterSidesDataStruct>.ParallelWriter Writer;

    // Const
    [ReadOnly] public NativeArray<int3> neighbours;
    [ReadOnly] public NativeArray<int3> clusterSides;
    [ReadOnly] public NativeArray<int3> axis;
    [ReadOnly] public int3 chunkSize;
    [ReadOnly] public NativeArray<bool> blockTypesIsTransparent;

    public void Execute(int index)
    {
        var cluster = ClusterData[index];
        var clusterSize = cluster.Size;
        var blockId = cluster.BlockId;

        NativeArray<int3> clusterAxis = new NativeArray<int3>(6, Allocator.Temp);
        clusterAxis[0] = neighbours[0];
        clusterAxis[1] = neighbours[1] * clusterSize.z;
        clusterAxis[2] = neighbours[2] * clusterSize.y;
        clusterAxis[3] = neighbours[3];
        clusterAxis[4] = neighbours[4];
        clusterAxis[5] = neighbours[5] * clusterSize.x;

        NativeArray<int3> clusterSideSizes = new NativeArray<int3>(6, Allocator.Temp);
        clusterSideSizes[0] = clusterSides[0] * clusterSize + new int3(0, 0, 1);
        clusterSideSizes[1] = clusterSides[1] * clusterSize + new int3(0, 0, 1);
        clusterSideSizes[2] = clusterSides[2] * clusterSize + new int3(0, 1, 0);
        clusterSideSizes[3] = clusterSides[3] * clusterSize + new int3(0, 1, 0);
        clusterSideSizes[4] = clusterSides[4] * clusterSize + new int3(1, 0, 0);
        clusterSideSizes[5] = clusterSides[5] * clusterSize + new int3(1, 0, 0);

        for (int i = 0; i < 6; i++)
        {
            int3 startPosition = cluster.Position + clusterAxis[i];

            NativeArray<bool> blockChecked = new NativeArray<bool>(
                clusterSideSizes[i].x * clusterSideSizes[i].y * clusterSideSizes[i].z,
                Allocator.Temp
            );
            // size of side
            int3 size;

            for (int x = 0; x < clusterSideSizes[i].x; x++)
            {
                for (int y = 0; y < clusterSideSizes[i].y; y++)
                {
                    for (int z = 0; z < clusterSideSizes[i].z; z++)
                    {
                        int idx0 = x + (y + z * clusterSideSizes[i].y) * clusterSideSizes[i].x;
                        if (blockChecked[idx0]) continue;

                        int nBlockId = GetBlockId(startPosition, new int3(x, y, z), i);
                        // Check if hides side
                        if (!blockTypesIsTransparent[nBlockId] || blockId == nBlockId) continue;
                        
                        size = new int3(1, 1, 1);

                        for (int axisIndex = 0; axisIndex < 3; axisIndex++)
                        {
                            // Length of side in axisIndex axis
                            int nextCount = 1;

                            // While side can be bigger
                            while (true)
                            {
                                int3 shift = axis[axisIndex] * nextCount;
                                // Check if outside of cluster
                                if (shift.x >= clusterSideSizes[i].x - x ||
                                    shift.y >= clusterSideSizes[i].y - y ||
                                    shift.z >= clusterSideSizes[i].z - z) break;

                                bool flag = true;
                                int blocksToAddIndex = 0;
                                NativeArray<int> blocksToAdd = new NativeArray<int>(size[0] * size[1], Allocator.Temp);
                                // Check if can add to side block/Line/Rectangle
                                // Z (Equals to 1 for checking Z axis)
                                for (int wz = 0; wz < size[0]; wz++)
                                {
                                    // Y (Equals to 1 for checking Z and Y axis)
                                    for (int wy = 0; wy < size[1]; wy++)
                                    {
                                        int3 pos = new int3(x + shift.x, y + wy + shift.y, z + wz + shift.z);
                                        int idx = pos.x + (pos.y + pos.z * clusterSideSizes[i].y) * clusterSideSizes[i].x;
                                        int blockCheckedId = GetBlockId(startPosition, pos, i);
                                        // Check if block belongs to another side or is different type
                                        if (!blockTypesIsTransparent[blockCheckedId] || blockCheckedId == blockId || blockChecked[idx])
                                        {
                                            flag = false;
                                            break;
                                        }
                                        blocksToAdd[blocksToAddIndex++] = idx;
                                    }
                                    if (!flag) break;
                                }
                                if (!flag) break;

                                // Increase length of side in axisIndex axis
                                nextCount++;
                                // Add blocks to blockChecked
                                foreach (int blockIndex in blocksToAdd)
                                {
                                    blockChecked[blockIndex] = true;
                                }
                                blocksToAdd.Dispose();
                            }
                            // Set side size in axisIndex axis
                            size[axisIndex] = nextCount;
                        }
                        // Create side
                        var data = new ClusterSidesDataStruct {
                            BlockId = cluster.BlockId,
                            Position = startPosition + new int3(x, y, z) - neighbours[i],
                            Rotation = i,
                            Size = new int3(size.z, size.y, size.x)
                        };
                        Writer.AddNoResize(data);
                    }
                }
            }
            blockChecked.Dispose();
        }
    }

    private int GetBlockId(int3 startPosition, int3 pos, int i)
    {
        int neighbourBlockId;
        if (startPosition.x < 0 || startPosition.x >= chunkSize.x ||
            startPosition.y < 0 || startPosition.y >= chunkSize.y ||
            startPosition.z < 0 || startPosition.z >= chunkSize.z)
        {
            int3 blockPos = (startPosition % chunkSize + chunkSize) % chunkSize + pos;
            int idx = blockPos.x + (blockPos.y + blockPos.z * chunkSize.y) * chunkSize.x;

            neighbourBlockId = chunkNeighbourData.ChunkNeighbourDataArray[i][idx];
        }
        else
        {
            int3 blockPos = startPosition + pos;
            int idx = blockPos.x + (blockPos.y + blockPos.z * chunkSize.y) * chunkSize.x;

            neighbourBlockId = blockIdDatas[idx];
        }
        return neighbourBlockId;
    }
}
