using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkV6
{
    private int3 coordinates;
    public float3 ChunkPosition
    {
        get { return new float3(coordinates.x, coordinates.y, coordinates.z) * Size; }
    }

    private WorldClassV3 world;

    private GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    // Blocks
    public static int cubeSize = 16;
    public static int3 Size = new int3(cubeSize, cubeSize, cubeSize);
    private NativeArray<int> blocks;

    // Clusters
    private NativeArray<int> blocksClusterIdDatas;
    private NativeList<int> clusterBlockIdDatas;
    private NativeList<int3> clusterSizeDatas;
    private NativeList<int3> clusterPositionDatas;
    private NativeList<ClusterSidesVisibility> clusterSidesVisibilityData;

    // Const
    private NativeArray<int3> axisArray;

    private NativeArray<int3> voxelNeighbours;
    private NativeArray<float3> voxelVerts;
    private int voxelTrisSize;
    private NativeArray<int> voxelTris;
    private NativeArray<float2> voxelUvs;
    private NativeArray<int> triangleOrder;

    private NativeArray<VertexAttributeDescriptor> layout;

    private NativeArray<int3> clusterSides;

    private float time;

    // Jobs
    private bool requireUpdate;
    private bool generatingBlockIds;
    private bool generatingClusters;
    private bool checkingVisibility;
    private bool generatingMesh;
    private Mesh.MeshDataArray meshDataArray;
    private JobHandle generatingBlockIdJobHandle;
    private JobHandle generatingClastersJobHandle;
    private JobHandle checkingVisibilityJobHandle;
    private JobHandle generatingMeshJobHandle;

    public ChunkV6(Vector3Int _position, WorldClassV3 _world)
    {
        coordinates = new int3(_position.x, _position.y, _position.z);
        world = _world;

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = Vector3.Scale(new Vector3(coordinates.x, coordinates.y, coordinates.z), new Vector3(Size.x, Size.y, Size.z));
        chunkObject.name = string.Format("Chunk {0}, {1}, {2}", coordinates.x, coordinates.y, coordinates.z);

        CreateConst();

        GenerateBlockIdWithJobs();
        generatingBlockIds = true;
    }

    private void CreateConst()
    {
        axisArray = new NativeArray<int3>(VoxelDataV2.axisArray, Allocator.Persistent);

        voxelNeighbours = new NativeArray<int3>(VoxelDataV2.voxelNeighbours, Allocator.Persistent);
        voxelVerts = new NativeArray<float3>(VoxelDataV2.voxelVerts, Allocator.Persistent);
        voxelUvs = new NativeArray<float2>(VoxelDataV2.voxelUvs, Allocator.Persistent);
        voxelTris = new NativeArray<int>(VoxelDataV2.voxelTris, Allocator.Persistent);
        voxelTrisSize = VoxelDataV2.voxelTrisSize;
        triangleOrder = new NativeArray<int>(VoxelDataV2.triangleOrder, Allocator.Persistent);

        layout = new NativeArray<VertexAttributeDescriptor>(VoxelDataV2.layoutVertex, Allocator.Persistent);


        int3[] clusterSidesArray = new int3[]
        {
            new int3(1, 1, 0),
            new int3(1, 1, 0),
            new int3(1, 0, 1),
            new int3(1, 0, 1),
            new int3(0, 1, 1),
            new int3(0, 1, 1)
        };
        clusterSides = new NativeArray<int3>(clusterSidesArray, Allocator.Persistent);

    }

    public void Destroy()
    {
        axisArray.Dispose();

        voxelNeighbours.Dispose();
        voxelVerts.Dispose();
        voxelUvs.Dispose();
        voxelTris.Dispose();
        triangleOrder.Dispose();

        layout.Dispose();
    }

    public void GenerateClastersWithJobs()
    {
        if (generatingBlockIds && generatingBlockIdJobHandle.IsCompleted)
        {
            generatingBlockIdJobHandle.Complete();
            generatingBlockIds = false;
            requireUpdate = true;
        }
        if (!requireUpdate) return;
        requireUpdate = false;
        generatingClusters = true;
        CreateClustersWithJobs();
    }

    public void GenerateMeshWithJobs()
    {
        if (!checkingVisibility && checkingVisibilityJobHandle.IsCompleted) return;
        checkingVisibilityJobHandle.Complete();
        checkingVisibility = false;
        generatingMesh = true;
        PrepareMeshWithClustersWithJobsGetData();
    }

    private void GenerateBlockIdWithJobs()
    {
        blocks = new NativeArray<int>(Size.x * Size.y * Size.z, Allocator.Persistent);

        GenerateBlockIdJob generateBlockIdJob = new GenerateBlockIdJob
        {
            blockIdDatas = blocks,

            chunkSize = Size,
            chunkPosition = ChunkPosition
        };

        generatingBlockIdJobHandle = generateBlockIdJob.Schedule(Size.x * Size.y * Size.z, 32);
    }

    [BurstCompile]
    private struct GenerateBlockIdJob : IJobParallelFor
    {
        public NativeArray<int> blockIdDatas;

        [ReadOnly] public int3 chunkSize;
        [ReadOnly] public float3 chunkPosition;
        public void Execute(int index)
        {
            int x = index % chunkSize.x;
            int y = ((index - x) / chunkSize.x) % chunkSize.y;
            int z = (((index - x) / chunkSize.x) - y) / chunkSize.y;
            float3 position = new float3(x, y, z) + chunkPosition;
            float scale = 0.2f;
            float offset = 0;
            int terrainHeightDifference = 4;
            int terrainSolidGround = 12;

            int yPos = (int)math.floor(position.y);

            byte voxelValue = 0;

            int terrainHeight = (int)math.floor((noise.snoise(
                new float2(
                    position.x / chunkSize.x * scale + offset,
                    position.z / chunkSize.z * scale + offset)) + 1.0) / 2 * terrainHeightDifference) + terrainSolidGround;

            if (yPos > terrainHeight)
                voxelValue = 0;
            else if (yPos == terrainHeight)
                voxelValue = 3;
            else if (yPos > terrainHeight - 4)
                voxelValue = 4;
            else if (yPos < terrainHeight)
                voxelValue = 2;

            blockIdDatas[index] = voxelValue;
        }
    }

    private void CreateClustersWithJobs()
    {
        blocksClusterIdDatas = new NativeArray<int>(blocks.Length, Allocator.Persistent);
        clusterBlockIdDatas = new NativeList<int>(Allocator.Persistent);
        clusterPositionDatas = new NativeList<int3>(Allocator.Persistent);
        clusterSizeDatas = new NativeList<int3>(Allocator.Persistent);
        clusterSidesVisibilityData = new NativeList<ClusterSidesVisibility>(Allocator.Persistent);

        CreateClastersJob createClastersJob = new CreateClastersJob
        {
            blockIdDatas = blocks,

            blocksClusterIdDatas = blocksClusterIdDatas,
            clusterBlockIdDatas = clusterBlockIdDatas,
            clusterPositionDatas = clusterPositionDatas,
            clusterSizeDatas = clusterSizeDatas,
            clusterSidesVisibilityData = clusterSidesVisibilityData,

            axis = axisArray,
            chunkSize = Size
        };
        generatingClastersJobHandle = createClastersJob.Schedule();
    }

    [BurstCompile]
    private struct CreateClastersJob : IJob
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

    public void CheckClusterVisibilityWithJobs()
    {
        if (!generatingClusters && generatingClastersJobHandle.IsCompleted) return;
        generatingClastersJobHandle.Complete();
        generatingClusters = false;
        checkingVisibility = true;

        CheckClusterVisibilityJob checkClusterVisibilityJob = new CheckClusterVisibilityJob
        {
            blockIdDatas = blocks,
            clusterBlockIdDatas = clusterBlockIdDatas,
            clusterPositionDatas = clusterPositionDatas,
            clusterSidesVisibilityData = clusterSidesVisibilityData,
            clusterSizeDatas = clusterSizeDatas,
            neighbours = voxelNeighbours,
            clusterSides = clusterSides,
            chunkSize = Size
        };
        checkingVisibilityJobHandle = checkClusterVisibilityJob.Schedule(clusterPositionDatas.Length, 16);
    }

    [BurstCompile]
    private struct CheckClusterVisibilityJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> blockIdDatas;

        [ReadOnly] public NativeArray<int> clusterBlockIdDatas;
        [ReadOnly] public NativeArray<int3> clusterSizeDatas;
        [ReadOnly] public NativeArray<int3> clusterPositionDatas;

        [ReadOnly] public NativeArray<int3> neighbours;
        [ReadOnly] public NativeArray<int3> clusterSides;
        [ReadOnly] public int3 chunkSize;


        public NativeArray<ClusterSidesVisibility> clusterSidesVisibilityData;

        public void Execute(int index)
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
    }

    private void PrepareMeshWithClustersWithJobsGetData()
    {
        // Allocate mesh to create
        meshDataArray = Mesh.AllocateWritableMeshData(1);
        //NativeArray<int> blocksTmp = new NativeArray<int>(blocks, Allocator.TempJob);

        CreateMeshWithClustersJob createMeshWithClustersJob = new CreateMeshWithClustersJob
        {
            // Data
            clusterBlockIdDatas = clusterBlockIdDatas,
            clusterSizeDatas = clusterSizeDatas,
            clusterPositionDatas = clusterPositionDatas,
            clusterSidesVisibilityData = clusterSidesVisibilityData,

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
        generatingMeshJobHandle = createMeshWithClustersJob.Schedule();
    }

    [BurstCompile]
    private struct CreateMeshWithClustersJob : IJob
    {
        // Input data
        [ReadOnly] public NativeArray<int> clusterBlockIdDatas;
        [ReadOnly] public NativeArray<int3> clusterSizeDatas;
        [ReadOnly] public NativeArray<int3> clusterPositionDatas;
        [ReadOnly] public NativeArray<ClusterSidesVisibility> clusterSidesVisibilityData;

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

            for (int index = 0; index < clusterBlockIdDatas.Length; index++)
            {
                if (blockTypesIsSold[clusterBlockIdDatas[index]])
                {
                    for (int j = 0; j < 6; j++)
                    {
                        int3 actualSize = clusterSizeDatas[index];
                        int perfectSize = 0;
                        switch (j)
                        {
                            case 0: perfectSize = actualSize.x * actualSize.y; break;
                            case 1: perfectSize = actualSize.x * actualSize.y; break;
                            case 2: perfectSize = actualSize.x * actualSize.z; break;
                            case 3: perfectSize = actualSize.x * actualSize.z; break;
                            case 4: perfectSize = actualSize.y * actualSize.z; break;
                            case 5: perfectSize = actualSize.y * actualSize.z; break;
                            default:
                                break;
                        }

                        if (clusterSidesVisibilityData[index][j] >= perfectSize) continue;
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
            for (int index = 0; index < clusterBlockIdDatas.Length; index++)
            {
                if (blockTypesIsSold[clusterBlockIdDatas[index]])
                {
                    for (int j = 0; j < 6; j++)
                    {
                        int3 actualSize = clusterSizeDatas[index];
                        int perfectSize = 0;
                        switch (j)
                        {
                            case 0: perfectSize = actualSize.x * actualSize.y; break;
                            case 1: perfectSize = actualSize.x * actualSize.y; break;
                            case 2: perfectSize = actualSize.x * actualSize.z; break;
                            case 3: perfectSize = actualSize.x * actualSize.z; break;
                            case 4: perfectSize = actualSize.y * actualSize.z; break;
                            case 5: perfectSize = actualSize.y * actualSize.z; break;
                            default:
                                break;
                        }

                        if (clusterSidesVisibilityData[index][j] >= perfectSize) continue;

                        int blockID = clusterBlockIdDatas[index];
                        float3 position = clusterPositionDatas[index];// - chunkPos;

                        for (int k = 0; k < 4; k++)
                        {
                            vertex[vertexIndex + k] = new VertexPositionUvStruct
                            {
                                pos = position + voxelVerts[voxelTris[j * voxelTrisSize + k]] * clusterSizeDatas[index],
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

    public float LoadMesh()
    {
        if (!generatingMesh || !generatingMeshJobHandle.IsCompleted)
            return 0f;

        generatingMesh = false;
        generatingMeshJobHandle.Complete();

        Mesh mesh = new Mesh();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        meshRenderer.materials = world.materials.ToArray();
        return 0;
    }

    public int GetIndex(int3 position)
    {
        return position.x +
            (position.y + position.z * Size.y) * Size.x;
    }

    public int SetBlock(int3 position, int blockID)
    {
        if (generatingBlockIds || generatingClusters || generatingMesh) return 0;
        
        int oldBlockID = blocks[GetIndex(position)];
        blocks[GetIndex(position)] = blockID;
        requireUpdate = true;
        Debug.Log(string.Format("Block Placed at: {0}, {1}, {2}", position.x, position.y, position.z));

        return oldBlockID;
    }

    public bool TryPlaceBlock(int3 position, int blockID)
    {
        if (generatingBlockIds || generatingClusters || generatingMesh) return false;

        if (blocks[GetIndex(position)] == 0) return false;
        blocks[GetIndex(position)] = blockID;
        requireUpdate = true;
        Debug.Log(string.Format("Block Placed at: {0}, {1}, {2}", position.x, position.y, position.z));

        return true;
    }

    public int GetBlock(int3 position)
    {
        if (generatingBlockIds || generatingClusters || generatingMesh) return 0;
        return blocks[GetIndex(position)];
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct VertexPositionUvStruct
    {
        public float3 pos;
        public float2 uv;
    }
}
