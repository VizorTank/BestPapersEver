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
    [ReadOnly] public NativeArray<ClusterSidesDataStruct> ClusterSidesData;

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
        int2 perfectSize = new int2();
        // for (int index = 0; index < ClusterData.Length; index++)
        for (int index = 0; index < ClusterSidesData.Length; index++)
        {
            // var cluster = ClusterData[index];
            var cluster = ClusterSidesData[index];
            if (!blockTypesIsInvisible[cluster.BlockId])
            {
                sidesCount++;
                sidesCountPerType[cluster.BlockId]++;
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
        
        // for (int index = 0; index < ClusterData.Length; index++)
        for (int index = 0; index < ClusterSidesData.Length; index++)
        {
            // var cluster = ClusterData[index];
            var cluster = ClusterSidesData[index];
            if (!blockTypesIsInvisible[cluster.BlockId])
            {
                int j = cluster.Rotation;
                int3 actualSize = cluster.Size;
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
                
                int blockID = cluster.BlockId;
                float3 position = cluster.Position;

                for (int k = 0; k < 4; k++)
                {
                    vertex[vertexIndex + k] = new VertexPositionUvStruct
                    {
                        pos = position + voxelVerts[voxelTris[j * voxelTrisSize + k]] * cluster.Size,
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
