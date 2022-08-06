using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[BurstCompile]
public struct CreateMeshWithClustersJob : IJob
{
    // Input data
    /*[ReadOnly] public NativeArray<int> clusterBlockIdDatas;
    [ReadOnly] public NativeArray<int3> clusterSizeDatas;
    [ReadOnly] public NativeArray<int3> clusterPositionDatas;
    [ReadOnly] public NativeArray<ClusterSidesVisibility> clusterSidesVisibilityData;*/

    [ReadOnly] public NativeArray<ClusterCreationStruct> ClusterData;

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
    [ReadOnly] public NativeArray<bool> blockTypesIsInvisible;

    // Chunk position
    [ReadOnly] public float3 chunkPos;
    [ReadOnly] public int3 chunkSize;

    // How data is inserted to MeshData
    [ReadOnly] public NativeArray<VertexAttributeDescriptor> layout;

    // Output mesh
    public Mesh.MeshData data;

    public void Execute()
    {
        // Count sides and sides per block type
        NativeArray<int> sidesCountPerType = new NativeArray<int>(blockTypesCount, Allocator.Temp);
        int sidesCount = 0;

        for (int index = 0; index < ClusterData.Length; index++)
        {
            if (!blockTypesIsInvisible[ClusterData[index].BlockId])
            {
                for (int j = 0; j < 6; j++)
                {
                    int3 actualSize = ClusterData[index].Size;
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

                    if (ClusterData[index].Visibility[j] >= maxSize) continue;
                    sidesCount++;
                    sidesCountPerType[ClusterData[index].BlockId]++;
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
        for (int index = 0; index < ClusterData.Length; index++)
        {
            if (!blockTypesIsInvisible[ClusterData[index].BlockId])
            {
                for (int j = 0; j < 6; j++)
                {
                    int3 actualSize = ClusterData[index].Size;
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

                    if (ClusterData[index].Visibility[j] >= perfectSize.x * perfectSize.y) continue;

                    int blockID = ClusterData[index].BlockId;
                    float3 position = ClusterData[index].Position;

                    for (int k = 0; k < 4; k++)
                    {
                        vertex[vertexIndex + k] = new VertexPositionUvStruct
                        {
                            pos = position + voxelVerts[voxelTris[j * voxelTrisSize + k]] * ClusterData[index].Size,
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
    }
}
