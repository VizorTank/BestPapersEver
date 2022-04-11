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
    public float3 ChunkPosition => new float3(coordinates.x, coordinates.y, coordinates.z) * Size;

    private WorldClass world;

    private GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    public ChunkNeighbours neighbours;
    private ChunkNeighbourData ChunkNeighbourData;

    private BiomeAttributesStruct biome;

    // Blocks
    private static int3 Size { get => VoxelData.ChunkSize; }
    private NativeArray<int> blocksId;
    private NativeArray<int> blocksForMeshGeneration;

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
    public int usedByOtherChunks = 0;

    private bool updating;
    private bool requireUpdate;
    private bool readyForUpdate;
    private bool generatingBlockIds;
    private bool generatingClusters;
    private bool checkingVisibility;
    private bool generatingMesh;
    private Mesh.MeshDataArray meshDataArray;
    private JobHandle generatingBlockIdJobHandle;
    private JobHandle generatingClastersJobHandle;
    private JobHandle checkingVisibilityJobHandle;
    private JobHandle generatingMeshJobHandle;

    public Chunk(int3 _position, WorldClass _world, BiomeAttributesStruct _biome)
    {
        coordinates = _position;
        world = _world;
        biome = _biome;

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

        clusterSides = new NativeArray<int3>(VoxelData.clusterSidesArray, Allocator.Persistent);

        blocksForMeshGeneration = default;
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

        blocksId.Dispose();

        blocksClusterIdDatas.Dispose();
        clusterBlockIdDatas.Dispose();
        clusterSizeDatas.Dispose();
        clusterPositionDatas.Dispose();
        clusterSidesVisibilityData.Dispose();

        blocksForMeshGeneration.Dispose();
    }

    public void CreateBlockIdCopy()
    {
        if (generatingBlockIds)
        {
            generatingBlockIdJobHandle.Complete();
            generatingBlockIds = false;
            requireUpdate = true;
        }
        if (!requireUpdate) return;
        if (usedByOtherChunks > 0) return;
        requireUpdate = false;
        readyForUpdate = true;
        updating = true;
        blocksForMeshGeneration.Dispose();
        blocksForMeshGeneration = default;
        blocksForMeshGeneration = new NativeArray<int>(blocksId, Allocator.Persistent);
    }

    public bool CanGetBlocks => !updating;
    public NativeArray<int> GetBlocks()
    {
        if (updating) return new NativeArray<int>(0, Allocator.Persistent);
        return blocksForMeshGeneration;
    }

    public void GetNeighboursData()
    {
        ChunkNeighbourData = new ChunkNeighbourData();
        for (int i = 0; i < 6; i++)
        {
            if (neighbours[i] == null)
            {
                ChunkNeighbourData[i] = new NativeArray<int>(0, Allocator.Persistent);
                continue;
            }
            neighbours[i].usedByOtherChunks++;
            ChunkNeighbourData[i] = neighbours[i].GetBlocks();
        }
    }

    public void FreeNeighboursData()
    {
        for (int i = 0; i < 6; i++)
        {
            if (neighbours[i] == null) continue;
            neighbours[i].usedByOtherChunks--;
        }
    }

    public void CreateMesh()
    {
        if (!readyForUpdate) return;
        readyForUpdate = false;
        generatingMesh = true;
        meshDataArray = Mesh.AllocateWritableMeshData(1);
        CreateMeshJob createMeshJob = new CreateMeshJob
        {
            blockIdDatas = blocksForMeshGeneration,

            axis = axisArray,

            neighbours = voxelNeighbours,
            voxelTris = voxelTris,
            voxelTrisSize = voxelTrisSize,
            voxelUvs = voxelUvs,
            voxelVerts = voxelVerts,

            triangleOrder = triangleOrder,

            // Block Types Count
            blockTypesCount = world.blockTypesList.areSolid.Length,
            blockTypesIsSold = world.blockTypesList.areSolid,

            // How data is inserted to MeshData
            layout = layout,

            // Chunk position
            chunkSize = Size,
            clusterSides = clusterSides,
            
            data = meshDataArray[0]
        };

        generatingMeshJobHandle = createMeshJob.Schedule();
    }

    public void CreateClasters()
    {
        if (generatingBlockIds && generatingBlockIdJobHandle.IsCompleted)
        {
            generatingBlockIdJobHandle.Complete();
            generatingBlockIds = false;
            CreateBlockIdCopy();
            requireUpdate = true;
        }
        if (!readyForUpdate) return;
        readyForUpdate = false;
        generatingClusters = true;

        blocksClusterIdDatas = new NativeArray<int>(blocksId.Length, Allocator.Persistent);
        clusterBlockIdDatas = new NativeList<int>(Allocator.Persistent);
        clusterPositionDatas = new NativeList<int3>(Allocator.Persistent);
        clusterSizeDatas = new NativeList<int3>(Allocator.Persistent);
        clusterSidesVisibilityData = new NativeList<ClusterSidesVisibility>(Allocator.Persistent);

        CreateClastersJob createClastersJob = new CreateClastersJob
        {
            blockIdDatas = blocksForMeshGeneration,

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

    public void CheckClusterVisibility()
    {
        if (!generatingClusters && generatingClastersJobHandle.IsCompleted) return;
        generatingClastersJobHandle.Complete();
        generatingClusters = false;
        checkingVisibility = true;
        GetNeighboursData();

        CheckClusterVisibilityJob checkClusterVisibilityJob = new CheckClusterVisibilityJob
        {
            blockIdDatas = blocksForMeshGeneration,

            clusterBlockIdDatas = clusterBlockIdDatas,
            clusterPositionDatas = clusterPositionDatas,
            clusterSidesVisibilityData = clusterSidesVisibilityData,
            clusterSizeDatas = clusterSizeDatas,

            blockTypesIsTransparent = world.blockTypesList.areTransparent,

            chunkNeighbourData = ChunkNeighbourData,

            neighbours = voxelNeighbours,
            clusterSides = clusterSides,
            chunkSize = Size
        };
        checkingVisibilityJobHandle = checkClusterVisibilityJob.Schedule(clusterPositionDatas.Length, 16);
    }

    public void CreateMeshWithClasters()
    {
        if (!checkingVisibility && checkingVisibilityJobHandle.IsCompleted) return;
        checkingVisibilityJobHandle.Complete();
        FreeNeighboursData();
        checkingVisibility = false;
        generatingMesh = true;

        // Allocate mesh to create
        meshDataArray = Mesh.AllocateWritableMeshData(1);

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
            blockTypesCount = world.blockTypesList.BlockCount,
            blockTypesIsInvisible = world.blockTypesList.areInvisible,

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
        updating = false;
        generatingMeshJobHandle.Complete();

        Mesh mesh = new Mesh();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        //meshCollider.sharedMesh = mesh;

        meshRenderer.materials = world.Materials.ToArray();
        return 0;
    }

    private void GenerateBlockIdWithJobs()
    {
        blocksId = new NativeArray<int>(Size.x * Size.y * Size.z, Allocator.Persistent);

        GenerateBlockIdJob generateBlockIdJob = new GenerateBlockIdJob
        {
            blockIdDatas = blocksId,

            biome = biome,

            chunkSize = Size,
            chunkPosition = ChunkPosition
        };

        generatingBlockIdJobHandle = generateBlockIdJob.Schedule(Size.x * Size.y * Size.z, 32);
    }

    public int GetIndex(int3 position)
    {
        return position.x +
            (position.y + position.z * Size.y) * Size.x;
    }

    public int SetBlock(int3 position, int blockID)
    {
        if (generatingBlockIds || generatingClusters || generatingMesh) return 0;
        
        int oldBlockID = blocksId[GetIndex(position)];
        blocksId[GetIndex(position)] = blockID;
        requireUpdate = true;
        Debug.Log(string.Format("Block Placed at: {0}, {1}, {2}", position.x, position.y, position.z));

        return oldBlockID;
    }

    public bool TryPlaceBlock(int3 position, int blockID)
    {
        if (generatingBlockIds || generatingClusters || generatingMesh) return false;

        if (blocksId[GetIndex(position)] == 0) return false;
        blocksId[GetIndex(position)] = blockID;
        requireUpdate = true;
        Debug.Log(string.Format("Block Placed at: {0}, {1}, {2}", position.x, position.y, position.z));

        return true;
    }

    public int GetBlock(int3 position)
    {
        if (generatingBlockIds || generatingClusters || generatingMesh) return 0;
        return blocksId[GetIndex(position)];
    }
}
