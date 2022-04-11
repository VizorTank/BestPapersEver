using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public struct CreateMeshJob : IJob
{
    // Input
    [ReadOnly] public NativeArray<int> blockIdDatas;

    // Const
    [ReadOnly] public NativeArray<int3> axis;
    [ReadOnly] public int3 chunkSize;
    [ReadOnly] public NativeArray<int3> clusterSides;

    // Static values
    // Block attributes
    [ReadOnly] public NativeArray<int3> neighbours;
    [ReadOnly] public NativeArray<float3> voxelVerts;
    [ReadOnly] public int voxelTrisSize;
    [ReadOnly] public NativeArray<int> voxelTris;
    [ReadOnly] public NativeArray<float2> voxelUvs;

    // Order of vertices of triangles on side
    [ReadOnly] public NativeArray<int> triangleOrder;


    // Block type count
    [ReadOnly] public int blockTypesCount;
    [ReadOnly] public NativeArray<bool> blockTypesIsSold;

    // How data is inserted to MeshData
    [ReadOnly] public NativeArray<VertexAttributeDescriptor> layout;

    // Output mesh
    public Mesh.MeshData data;
    public void Execute()
    {
        //NativeArray<int> blocksClusterIdDatas = new NativeArray<int>(blockIdDatas.Length, Allocator.Temp);
        NativeList<int> clusterBlockIdDatas = new NativeList<int>(Allocator.Temp);
        NativeList<int3> clusterSizeDatas = new NativeList<int3>(Allocator.Temp);
        NativeList<int3> clusterPositionDatas = new NativeList<int3>(Allocator.Temp);
        NativeList<ClusterSidesVisibility> clusterSidesVisibilityData = new NativeList<ClusterSidesVisibility>(Allocator.Temp); ;
        
        //---------------------------------------------
        //              Create Clusters
        //---------------------------------------------
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
                                //blocksClusterIdDatas[blockIndex] = clusterId;
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
                    clusterSidesVisibilityData.Add(clusterSidesVisibility);

                    clusterSize.Dispose();
                }
            }
        }
        blockChecked.Dispose();

        //---------------------------------------------
        //          Check Clusters Visibility
        //---------------------------------------------

        for (int index = 0; index < clusterSizeDatas.Length; index++)
        {
            int3 clusterSize = clusterSizeDatas[index];

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
                int3 startPosition = clusterPositionDatas[index] + axis[i];
                if (startPosition.x < 0 || startPosition.x >= chunkSize.x ||
                    startPosition.y < 0 || startPosition.y >= chunkSize.y ||
                    startPosition.z < 0 || startPosition.z >= chunkSize.z) continue;

                for (int x = 0; x < clusterSideSizes[i].x; x++)
                {
                    for (int y = 0; y < clusterSideSizes[i].y; y++)
                    {
                        for (int z = 0; z < clusterSideSizes[i].z; z++)
                        {
                            int3 blockPos = startPosition + new int3(x, y, z);
                            int idx = blockPos.x + (blockPos.y + blockPos.z * chunkSize.y) * chunkSize.x;

                            if (blockIdDatas[idx] != 0) clusterSidesVisibility[i]++;
                        }
                    }
                }
            }
            clusterSidesVisibilityData[index] = clusterSidesVisibility;
        }

        //---------------------------------------------
        //          Create Mesh with Clusters
        //---------------------------------------------
        // Count sides and sides per block type
        NativeArray<int> sidesCountPerType = new NativeArray<int>(blockTypesCount, Allocator.Temp);
        int sidesCount = 0;

        for (int index = 0; index < clusterBlockIdDatas.Length; index++)
        {
            if (blockTypesIsSold[clusterBlockIdDatas[index]])
            {
                for (int j = 0; j < 6; j++)
                {
                    int3 actualSize = clusterSizeDatas[index];
                    int maxSize = 0;
                    switch (j)
                    {
                        case 0: maxSize = actualSize.x * actualSize.y; break;
                        case 1: maxSize = actualSize.x * actualSize.y; break;
                        case 2: maxSize = actualSize.x * actualSize.z; break;
                        case 3: maxSize = actualSize.x * actualSize.z; break;
                        case 4: maxSize = actualSize.y * actualSize.z; break;
                        case 5: maxSize = actualSize.y * actualSize.z; break;
                        default:
                            break;
                    }

                    if (clusterSidesVisibilityData[index][j] >= maxSize) continue;
                    sidesCount++;
                    sidesCountPerType[clusterBlockIdDatas[index]]++;
                }
            }
        }


        // Table of sums of previous triangles
        NativeArray<int> trianglesCountPerTypeSum = new NativeArray<int>(blockTypesCount, Allocator.Temp);
        int sumOfPreviousTriangles = 0;
        for (int i = 0; i < blockTypesCount; i++)
        {
            trianglesCountPerTypeSum[i] = sumOfPreviousTriangles;
            sumOfPreviousTriangles += sidesCountPerType[i] * 6;
        }

        // Set data type and size for MeshData
        data.SetVertexBufferParams(sidesCount * 4, layout);
        NativeArray<VertexPositionUvStruct> vertex = data.GetVertexData<VertexPositionUvStruct>();

        // Set index type and size for MeshData
        data.SetIndexBufferParams(sidesCount * 6, IndexFormat.UInt32);
        NativeArray<int> indexes = data.GetIndexData<int>();

        // Indexes per Block Type
        NativeArray<int> triangleIndexes = new NativeArray<int>(blockTypesCount, Allocator.Temp);
        int vertexIndex = 0;

        // Foreach block
        int2 perfectSize = new int2();
        for (int index = 0; index < clusterBlockIdDatas.Length; index++)
        {
            if (blockTypesIsSold[clusterBlockIdDatas[index]])
            {
                for (int j = 0; j < 6; j++)
                {
                    int3 actualSize = clusterSizeDatas[index];
                    switch (j)
                    {
                        case 0: perfectSize.x = actualSize.x; perfectSize.y = actualSize.y; break;
                        case 1: perfectSize.x = actualSize.x; perfectSize.y = actualSize.y; break;
                        case 2: perfectSize.x = actualSize.x; perfectSize.y = actualSize.z; break;
                        case 3: perfectSize.x = actualSize.x; perfectSize.y = actualSize.z; break;
                        case 4: perfectSize.x = actualSize.z; perfectSize.y = actualSize.y; break;
                        case 5: perfectSize.x = actualSize.z; perfectSize.y = actualSize.y; break;
                        default: break;
                    }

                    if (clusterSidesVisibilityData[index][j] >= perfectSize.x * perfectSize.y) continue;

                    int blockID = clusterBlockIdDatas[index];
                    float3 position = clusterPositionDatas[index];

                    for (int k = 0; k < 4; k++)
                    {
                        vertex[vertexIndex + k] = new VertexPositionUvStruct
                        {
                            pos = position + voxelVerts[voxelTris[j * voxelTrisSize + k]] * clusterSizeDatas[index],
                            uv = voxelUvs[k] * perfectSize
                        };
                    }
                    for (int k = 0; k < 6; k++)
                    {
                        indexes[trianglesCountPerTypeSum[blockID] + triangleIndexes[blockID]++] = vertexIndex + triangleOrder[k];
                    }
                    vertexIndex += 4;
                }
            }
        }

        data.subMeshCount = blockTypesCount;
        for (int i = 0; i < blockTypesCount; i++)
        {
            data.SetSubMesh(i, new SubMeshDescriptor(trianglesCountPerTypeSum[i], triangleIndexes[i]));
        }

        trianglesCountPerTypeSum.Dispose();
        triangleIndexes.Dispose();
        sidesCountPerType.Dispose();

        clusterBlockIdDatas.Dispose();
        clusterSizeDatas.Dispose();
        clusterPositionDatas.Dispose();
        clusterSidesVisibilityData.Dispose();
    }
}
