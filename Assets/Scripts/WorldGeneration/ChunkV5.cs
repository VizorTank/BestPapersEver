using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkV5
{
    private float timeJobTook;

    private Vector3Int coordinates;
    private WorldClassV3 world;

    private GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    public static int cubeSize = 16;
    public static int3 Size = new int3(cubeSize, cubeSize, cubeSize);
    private NativeArray<int> blocks;

    private NativeArray<int> triangleOrder;

    // Back Front Top Bottom Left Right
    private NativeArray<int3> voxelNeighbours;
    private NativeArray<float3> voxelVerts;
    private int voxelTrisSize;
    private NativeArray<int> voxelTris;
    private NativeArray<float2> voxelUvs;
    private NativeArray<VertexAttributeDescriptor> layout;

    private bool requireUpdate;
    private bool updating;

    private Mesh.MeshDataArray meshDataArray;
    private CreateMeshJob createMeshJob;
    private JobHandle createMeshJobHandle;

    public float3 ChunkPosition
    {
        get { return new float3(coordinates.x, coordinates.y, coordinates.z) * Size; }
    }

    public ChunkV5(Vector3Int _position, WorldClassV3 _world)
    {
        coordinates = _position;
        world = _world;

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = Vector3.Scale(coordinates, new Vector3(Size.x, Size.y, Size.z));
        chunkObject.name = string.Format("Chunk {0}, {1}, {2}", coordinates.x, coordinates.y, coordinates.z);

        voxelNeighbours = new NativeArray<int3>(VoxelDataV2.voxelNeighbours, Allocator.Persistent);
        voxelVerts = new NativeArray<float3>(VoxelDataV2.voxelVerts, Allocator.Persistent);
        voxelUvs = new NativeArray<float2>(VoxelDataV2.voxelUvs, Allocator.Persistent);
        voxelTris = new NativeArray<int>(VoxelDataV2.voxelTris, Allocator.Persistent);
        voxelTrisSize = VoxelDataV2.voxelTrisSize;
        layout = new NativeArray<VertexAttributeDescriptor>(VoxelDataV2.layoutVertex, Allocator.Persistent);
        triangleOrder = new NativeArray<int>(VoxelDataV2.triangleOrder, Allocator.Persistent);

        blocks = new NativeArray<int>(Size.x * Size.y * Size.z, Allocator.Persistent);
        GenerateChunk();
        requireUpdate = true;
    }

    public void GenerateChunk()
    {
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    blocks[GetIndex(new int3(x, y, z))] = GenerateBlock(new int3(x, y, z) + ChunkPosition);
                }
            }
        }
    }

    public int GenerateBlock(float3 position)
    {
        float scale = 0.2f;
        float offset = 0;
        int terrainHeightDifference = 4;
        int terrainSolidGround = 12;

        int yPos = (int)math.floor(position.y);

        byte voxelValue = 0;

        int terrainHeight = (int)math.floor((noise.snoise(
            new float2(
                position.x / Size.x * scale + offset,
                position.z / Size.z * scale + offset)) + 1.0) / 2 * terrainHeightDifference) + terrainSolidGround;

        if (yPos > terrainHeight)
            voxelValue = 0;
        else if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos > terrainHeight - 4)
            voxelValue = 4;
        else if (yPos < terrainHeight)
            voxelValue = 2;

        return voxelValue;
    }

    public void Destroy()
    {
        voxelNeighbours.Dispose();
        voxelVerts.Dispose();
        voxelUvs.Dispose();
        voxelTris.Dispose();
        layout.Dispose();
        triangleOrder.Dispose();

        blocks.Dispose();
    }


    public int SetBlock(int3 position, int blockID)
    {
        createMeshJobHandle.Complete();
        int oldBlockID = blocks[GetIndex(position)];
        blocks[GetIndex(position)] = blockID;
        requireUpdate = true;
        Debug.Log("Block Placed at:" + string.Format("{0}, {1}, {2}", position.x, position.y, position.z));

        return oldBlockID;
    }

    public int GetIndex(int3 position)
    {
        return position.x + (position.y + position.z * Size.y) * Size.x;
    }

    public int GenerateMeshWithJobs()
    {
        if (updating || !requireUpdate)
            return 0;
        requireUpdate = false;
        updating = true;
        PrepareMeshWithJobsGetData();
        return 1;
    }

    private void PrepareMeshWithJobsGetData()
    {
        // Allocate mesh to create
        meshDataArray = Mesh.AllocateWritableMeshData(1);
        //NativeArray<int> blocksTmp = new NativeArray<int>(blocks, Allocator.TempJob);

        createMeshJob = new CreateMeshJob
        {
            // Data
            blockIdDatasReadOnly = blocks,

            // Const
            neighbours = voxelNeighbours,
            voxelTris = voxelTris,
            voxelTrisSize = voxelTrisSize,
            voxelUvs = voxelUvs,
            voxelVerts = voxelVerts,

            triangleOrder = triangleOrder,

            // Block Types Count
            blockTypesCount = world.blockTypes.Length,
            blockTypesIsSold = world.blockTypesDoP.areSolid,

            // How data is inserted to MeshData
            layout = layout,

            // Chunk position
            chunkPos = new float3(ChunkPosition),
            chunkSize = Size,

            // Mesh to create
            data = meshDataArray[0]
        };

        // Schedule job
        //createMeshJobHandle = createMeshJob.Schedule();
        createMeshJob.Execute();
        timeJobTook = Time.realtimeSinceStartup;
    }
    [BurstCompile]
    public struct CreateMeshJob : IJob
    {
        // Input data
        [ReadOnly] public NativeArray<int> blockIdDatasReadOnly;

        // Static values
        // Block attributes
        [ReadOnly] public NativeArray<int3> neighbours;
        [ReadOnly] public NativeArray<float3> voxelVerts;
        [ReadOnly] public NativeArray<int> voxelTris;
        [ReadOnly] public int voxelTrisSize;
        [ReadOnly] public NativeArray<float2> voxelUvs;

        // Order of vertices of triangles on side
        [ReadOnly] public NativeArray<int> triangleOrder;

        // Block type count
        [ReadOnly] public int blockTypesCount;
        [ReadOnly] public NativeArray<bool> blockTypesIsSold;

        // Chunk position
        [ReadOnly] public float3 chunkPos;
        [ReadOnly] public int3 chunkSize;

        // How data is inserted to MeshData
        [ReadOnly] public NativeArray<VertexAttributeDescriptor> layout;

        // Output mesh
        public Mesh.MeshData data;

        public void Execute()
        {
            int[] blockIdDatas = blockIdDatasReadOnly.ToArray();
            

            // Count sides and sides per block type
            NativeArray<int> sidesCountPerType = new NativeArray<int>(blockTypesCount, Allocator.Temp);
            int sidesCount = 0;
            for (int x = 0; x < chunkSize.x; x++)
            {
                for (int y = 0; y < chunkSize.y; y++)
                {
                    for (int z = 0; z < chunkSize.z; z++)
                    {
                        int i = x + (y + z * chunkSize.y) * chunkSize.x;
                        if (blockTypesIsSold[blockIdDatas[i]])
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                bool isBorderOfChunk = false;
                                if (x <= 0 && neighbours[j].x < 0) isBorderOfChunk = true;
                                if (x >= chunkSize.x - 1 && neighbours[j].x > 0) isBorderOfChunk = true;
                                if (y >= chunkSize.y - 1 && neighbours[j].y > 0) isBorderOfChunk = true;
                                if (y <= 0 && neighbours[j].y < 0) isBorderOfChunk = true;
                                if (z <= 0 && neighbours[j].z < 0) isBorderOfChunk = true;
                                if (z >= chunkSize.z - 1 && neighbours[j].z > 0) isBorderOfChunk = true;
                                int neighbourPos = i + neighbours[j].x + (neighbours[j].y + neighbours[j].z * chunkSize.y) * chunkSize.x;
                                if (!isBorderOfChunk && blockTypesIsSold[blockIdDatas[neighbourPos]])
                                {
                                    continue;
                                }
                                sidesCount++;
                                sidesCountPerType[blockIdDatas[i]]++;
                            }
                        }
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
            for (int x = 0; x < chunkSize.x; x++)
            {
                for (int y = 0; y < chunkSize.y; y++)
                {
                    for (int z = 0; z < chunkSize.z; z++)
                    {
                        int i = x + (y + z * chunkSize.y) * chunkSize.x;
                        if (blockTypesIsSold[blockIdDatas[i]])
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                bool isBorderOfChunk = false;
                                if (x <= 0 && neighbours[j].x < 0) isBorderOfChunk = true;
                                if (x >= chunkSize.x - 1 && neighbours[j].x > 0) isBorderOfChunk = true;
                                if (y >= chunkSize.y - 1 && neighbours[j].y > 0) isBorderOfChunk = true;
                                if (y <= 0 && neighbours[j].y < 0) isBorderOfChunk = true;
                                if (z <= 0 && neighbours[j].z < 0) isBorderOfChunk = true;
                                if (z >= chunkSize.z - 1 && neighbours[j].z > 0) isBorderOfChunk = true;
                                int neighbourPos = i + neighbours[j].x + (neighbours[j].y + neighbours[j].z * chunkSize.y) * chunkSize.x;
                                if (!isBorderOfChunk && blockTypesIsSold[blockIdDatas[neighbourPos]])
                                {
                                    continue;
                                }

                                int blockID = blockIdDatas[i];
                                float3 position = new float3(x, y, z);// - chunkPos;
                                for (int k = 0; k < 4; k++)
                                {
                                    vertex[vertexIndex + k] = new VertexPositionUvStruct
                                    {
                                        pos = position + voxelVerts[voxelTris[j * voxelTrisSize + k]],
                                        uv = voxelUvs[k]
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
                }
            }

            data.subMeshCount = blockTypesCount;
            for (int i = 0; i < blockTypesCount; i++)
            {
                data.SetSubMesh(i, new SubMeshDescriptor(trianglesCountPerTypeSum[i], triangleIndexes[i]));
            }

            //blockIdDatas.Dispose();
            trianglesCountPerTypeSum.Dispose();
            triangleIndexes.Dispose();
            sidesCountPerType.Dispose();
        }
    }

    public float GenerateMeshWithJobsGetData2()
    {
        if (!updating || !createMeshJobHandle.IsCompleted)
            return 0f;

        updating = false;
        float t = Time.realtimeSinceStartup - timeJobTook;
        Debug.Log("Competed Job: " + t);
        createMeshJobHandle.Complete();

        Mesh mesh = new Mesh();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        meshRenderer.materials = world.materials.ToArray();
        return t;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct VertexPositionUvStruct
    {
        public float3 pos;
        public float2 uv;
    }
}
