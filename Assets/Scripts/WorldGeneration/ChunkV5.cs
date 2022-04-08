using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkV5
{
    static readonly ProfilerMarker CreateCluster = new ProfilerMarker("CreateCluster");
    static readonly ProfilerMarker CreateClusterJobs = new ProfilerMarker("CreateClusterJobs");
    static readonly ProfilerMarker GenerateMap = new ProfilerMarker("GenerateMap");
    static readonly ProfilerMarker CreateMesh = new ProfilerMarker("CreateMesh");
    private float timeJobTook;

    private Vector3Int coordinates;
    private WorldClassV3 world;

    private GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    public static int cubeSize = 16;
    public static int3 Size = new int3(cubeSize, cubeSize, cubeSize);
    private NativeArray<int> blocks;

    // Clusters
    private NativeArray<int> blocksClusterIdDatas;
    private NativeList<int> clusterBlockIdDatas;
    private NativeList<int3> clusterSizeDatas;
    private NativeList<int3> clusterPositionDatas;
    private NativeArray<ClusterSidesVisibility> clusterSidesVisibilityData;

    NativeArray<int3> axisArray;

    // Back Front Top Bottom Left Right
    private NativeArray<int3> voxelNeighbours;
    private NativeArray<float3> voxelVerts;
    private int voxelTrisSize;
    private NativeArray<int> voxelTris;
    private NativeArray<float2> voxelUvs;
    private NativeArray<int> triangleOrder;

    private NativeArray<VertexAttributeDescriptor> layout;

    private bool requireUpdate;
    private bool updating;

    private Mesh.MeshDataArray meshDataArray;
    private CreateMeshJob createMeshJob;
    private JobHandle createMeshJobHandle;

    private CreateMeshWithClustersJob createMeshWithClustersJob;

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
        triangleOrder = new NativeArray<int>(VoxelDataV2.triangleOrder, Allocator.Persistent);
        
        layout = new NativeArray<VertexAttributeDescriptor>(VoxelDataV2.layoutVertex, Allocator.Persistent);

        blocks = new NativeArray<int>(Size.x * Size.y * Size.z, Allocator.Persistent);

        // Temp Created
        blocksClusterIdDatas = new NativeArray<int>();
        clusterBlockIdDatas = new NativeList<int>();
        clusterSizeDatas = new NativeList<int3>();
        clusterPositionDatas = new NativeList<int3>();
        clusterSidesVisibilityData = new NativeArray<ClusterSidesVisibility>();

        axisArray = new NativeArray<int3>(new int3[3] 
                { 
                    new int3(0, 0, 1), 
                    new int3(0, 1, 0), 
                    new int3(1, 0, 0) 
                }, Allocator.Persistent);

        GenerateMap.Begin();
        //GenerateChunk();
        GenerateChunkWithJobs();
        GenerateMap.End();

        //CreateCluster.Begin();
        //CreateClusters();
        //CreateCluster.End();
        //CreateClusterJobs.Begin();
        //CreateClustersWithJobs();
        //CreateClusterJobs.End();
        requireUpdate = true;
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

        blocksClusterIdDatas.Dispose();
        clusterBlockIdDatas.Dispose();
        clusterSizeDatas.Dispose();
        clusterPositionDatas.Dispose();
        clusterSidesVisibilityData.Dispose();
    }

    public struct GenerateMapJob : IJobParallelFor
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

    public void GenerateChunkWithJobs()
    {
        blocks = new NativeArray<int>(Size.x * Size.y * Size.z, Allocator.Persistent);

        GenerateMapJob generateMapJob = new GenerateMapJob
        {
            blockIdDatas = blocks,

            chunkSize = Size,
            chunkPosition = ChunkPosition
        };

        generateMapJob.Schedule(Size.x * Size.y * Size.z, 32).Complete();
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
        return position.x + 
            (position.y + position.z * Size.y) * Size.x;
    }

    public int GenerateMeshWithJobs()
    {
        if (updating || !requireUpdate)
            return 0;
        requireUpdate = false;
        updating = true;
        //PrepareMeshWithJobsGetData();
        CreateClustersWithJobs();
        CheckClustersSides();
        PrepareMeshWithClustersWithJobsGetData();
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
            blockIdDatas = blocks,

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
        CreateMesh.Begin();
        createMeshJob.Execute();
        CreateMesh.End();
        timeJobTook = Time.realtimeSinceStartup;
    }

    public void CreateClustersWithJobs()
    {
        blocksClusterIdDatas = new NativeArray<int>(blocks.Length, Allocator.Persistent);
        clusterBlockIdDatas = new NativeList<int>(Allocator.Persistent);
        clusterPositionDatas = new NativeList<int3>(Allocator.Persistent);
        clusterSizeDatas = new NativeList<int3>(Allocator.Persistent);
        CreateClastersJob createClastersJob = new CreateClastersJob
        {
            blockIdDatas = blocks,

            blocksClusterIdDatas = blocksClusterIdDatas,
            clusterBlockIdDatas = clusterBlockIdDatas,
            clusterPositionDatas = clusterPositionDatas,
            clusterSizeDatas = clusterSizeDatas,

            axis = axisArray,
            chunkSize = Size
        };
        CreateCluster.Begin();
        createClastersJob.Execute();
        CreateCluster.End();
        axisArray.Dispose();
    }

    public void CreateClusters()
    {
        // input
        NativeArray<int> blockIdDatas = blocks;
        NativeArray<bool> blockChecked = new NativeArray<bool>(blocks.Length, Allocator.Temp);
        // output

        blocksClusterIdDatas = new NativeArray<int>(blocks.Length, Allocator.Persistent);
        clusterBlockIdDatas = new NativeList<int>(Allocator.Persistent);
        clusterSizeDatas = new NativeList<int3>(Allocator.Persistent);
        clusterPositionDatas = new NativeList<int3>(Allocator.Persistent);

        int clusterSizeDatasIndex = 0;
        
        int3 chunkSize = Size;
        
        int3[] axis = new int3[3]
        {
            new int3(0, 0, 1),
            new int3(0, 1, 0),
            new int3(1, 0, 0)
        };

        for (int i = 0; i < blockChecked.Length; i++)
        {
            blockChecked[i] = false;
        }

        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                for (int z = 0; z < chunkSize.z; z++)
                {
                    int idx = GetIndex(new int3(x, y, z));
                    if (blockChecked[idx] || blockIdDatas[idx] == 0) continue;

                    // Unnessesary
                    blockChecked[idx] = true;

                    int blockId = blockIdDatas[idx];
                    int clusterId = clusterSizeDatasIndex;
                    
                    int[] clusterSize = new int[3] { 1, 1, 1 };

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
                            int[] blocksToAdd = new int[clusterSize[0] * clusterSize[1]];
                            // Check block/Line/Rectangle
                            // 11 for Z axis
                            // 1Z for Y axis
                            // YZ for X axis
                            // Z
                            for (int i = 0; i < clusterSize[0]; i++)
                            {
                                // Y
                                for (int j = 0; j < clusterSize[1]; j++)
                                {
                                    int3 pos = new int3(x + shift.x, y + j + shift.y, z + i + shift.z);
                                    int index = GetIndex(pos);
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
                        }
                        clusterSize[axisIndex] = nextCount;
                    }
                    clusterSizeDatas.Add(new int3(clusterSize[2], clusterSize[1], clusterSize[0]));
                    clusterPositionDatas.Add(new int3(x, y, z));
                    clusterBlockIdDatas.Add(blockId);
                    clusterSizeDatasIndex = clusterSizeDatas.Length;
                }
            }
        }
        blockChecked.Dispose();
    }

    public void CheckClustersSides()
    {
        clusterSidesVisibilityData = new NativeArray<ClusterSidesVisibility>(clusterBlockIdDatas.Length, Allocator.Persistent);

        for (int i = 0; i < clusterSidesVisibilityData.Length; i++)
        {
            ClusterSidesVisibility cluster = new ClusterSidesVisibility();
            for (int j = 0; j < 6; j++)
                cluster[j] = 0;
            clusterSidesVisibilityData[i] = cluster;
        }
    }

    private void PrepareMeshWithClustersWithJobsGetData()
    {
        // Allocate mesh to create
        meshDataArray = Mesh.AllocateWritableMeshData(1);
        //NativeArray<int> blocksTmp = new NativeArray<int>(blocks, Allocator.TempJob);

        createMeshWithClustersJob = new CreateMeshWithClustersJob
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
        //createMeshJobHandle = createMeshJob.Schedule();
        CreateMesh.Begin();
        createMeshWithClustersJob.Execute();
        CreateMesh.End();
        timeJobTook = Time.realtimeSinceStartup;
    }

    public struct CreateMeshWithClustersJob : IJob
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

                        NativeArray<int> clusterSize = new NativeArray<int>(3, Allocator.TempJob);
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
                                NativeArray<int> blocksToAdd = new NativeArray<int>(clusterSize[0] * clusterSize[1], Allocator.TempJob);
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

                        clusterSize.Dispose();
                    }
                }
            }
            blockChecked.Dispose();
        }
    }

    [BurstCompile]
    public struct CreateMeshJob : IJob
    {
        // Input data
        [ReadOnly] public NativeArray<int> blockIdDatas;

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
        //Debug.Log("Competed Job: " + t);
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
