using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class Chunk
{
    private int3 coordinates;
    public float3 ChunkPosition
    {
        get { return new float3(coordinates.x, coordinates.y, coordinates.z) * Size; }
    }

    private WorldClass world;

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

    public Chunk(Vector3Int _position, WorldClass _world)
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
        axisArray = new NativeArray<int3>(VoxelData.axisArray, Allocator.Persistent);

        voxelNeighbours = new NativeArray<int3>(VoxelData.voxelNeighbours, Allocator.Persistent);
        voxelVerts = new NativeArray<float3>(VoxelData.voxelVerts, Allocator.Persistent);
        voxelUvs = new NativeArray<float2>(VoxelData.voxelUvs, Allocator.Persistent);
        voxelTris = new NativeArray<int>(VoxelData.voxelTris, Allocator.Persistent);
        voxelTrisSize = VoxelData.voxelTrisSize;
        triangleOrder = new NativeArray<int>(VoxelData.triangleOrder, Allocator.Persistent);

        layout = new NativeArray<VertexAttributeDescriptor>(VoxelData.layoutVertex, Allocator.Persistent);


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

        clusterSides.Dispose();

        blocks.Dispose();

        blocksClusterIdDatas.Dispose();
        clusterBlockIdDatas.Dispose();
        clusterSizeDatas.Dispose();
        clusterPositionDatas.Dispose();
        clusterSidesVisibilityData.Dispose();
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
}
