using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkGenerator
{
    private Chunk chunk;
    private WorldClass world;

    private GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

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

    public ChunkNeighbours neighbours;
    private ChunkNeighbourData ChunkNeighbourData;

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

    private BiomeAttributesStruct biome;

    public ChunkGenerator(Chunk _chunk, WorldClass _world, BiomeAttributesStruct _biome)
    {
        chunk = _chunk;
        world = _world;
        biome = _biome;

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        chunkObject.transform.SetParent(world.transform);
        int3 chunkCoords = chunk.Coordinates;
        chunkObject.transform.position = Vector3.Scale(new Vector3(chunkCoords.x, chunkCoords.y, chunkCoords.z), new Vector3(Size.x, Size.y, Size.z));
        chunkObject.name = string.Format("Chunk {0}, {1}, {2}", chunkCoords.x, chunkCoords.y, chunkCoords.z);

        CreateConst();

        GenerateBlockIdWithJobs();
        generatingBlockIds = true;
        updating = true;
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
        DisposeArrays();

        ChunkNeighbourData.Destroy();

        UnityEngine.Object.Destroy(chunkObject);
    }

    private void DisposeArrays()
    {
        generatingBlockIdJobHandle.Complete();
        generatingClastersJobHandle.Complete();
        checkingVisibilityJobHandle.Complete();
        generatingMeshJobHandle.Complete();

        try { axisArray.Dispose(); } catch { }

        try { voxelNeighbours.Dispose(); } catch { }
        try { voxelVerts.Dispose(); } catch { }
        try { voxelUvs.Dispose(); } catch { }
        try { voxelTris.Dispose(); } catch { }
        try { triangleOrder.Dispose(); } catch { }
        try { layout.Dispose(); } catch { }

        try { clusterSides.Dispose(); } catch { }

        try { blocksId.Dispose(); } catch { }

        try { blocksClusterIdDatas.Dispose(); } catch { }
        try { clusterBlockIdDatas.Dispose(); } catch { }
        try { clusterSizeDatas.Dispose(); } catch { }
        try { clusterPositionDatas.Dispose(); } catch { }
        try { clusterSidesVisibilityData.Dispose(); } catch { }
        try { blocksForMeshGeneration.Dispose(); } catch { }

        try { meshDataArray.Dispose(); } catch { }
    }
    public void NeighbourDepecency(int value) => usedByOtherChunks += value;

    public bool CanEditChunk() => !updating;
    public void SetNeighbours(ChunkNeighbours chunkNeighbours) => neighbours = chunkNeighbours;
    public int GetBlock(int3 position)
    {
        if (!CanEditChunk()) throw new Exception("Trying getting a block when can't edit.");
        return blocksId[GetIndex(position)];
    }
    public int SetBlock(int3 position, int value)
    {
        if (!CanEditChunk()) throw new Exception("Trying setting a block when can't edit.");

        int oldBlockID = GetBlock(position);
        blocksId[GetIndex(position)] = value;
        requireUpdate = true;
        //MyLogger.Display(string.Format("Block Placed at: {0}, {1}, {2}", position.x, position.y, position.z));

        return oldBlockID;
    }

    public int GetIndex(int3 position) => position.x + (position.y + position.z * Size.y) * Size.x;
    
    public void CreateBlockIdCopy()
    {
        if (generatingBlockIds)
        {
            generatingBlockIdJobHandle.Complete();
            generatingBlockIds = false;
            updating = false;
            requireUpdate = true;
        }
        if (!requireUpdate) return;
        if (usedByOtherChunks > 0) return;
        requireUpdate = false;
        readyForUpdate = true;
        updating = true;
        try { blocksForMeshGeneration.Dispose(); } catch { }
        blocksForMeshGeneration = new NativeArray<int>(blocksId, Allocator.Persistent);
    }
    public bool CanGetBlocks => !updating;
    public NativeArray<int> GetBlocks()
    {
        if (updating) throw new Exception("Cant get blocks");
        return blocksForMeshGeneration;
    }
    public void GetNeighboursData()
    {
        ChunkNeighbourData = new ChunkNeighbourData();
        for (int i = 0; i < 6; i++)
        {
            if (neighbours == null || neighbours[i] == null || !neighbours[i].CanEditChunk())
            {
                ChunkNeighbourData[i] = new NativeArray<int>(0, Allocator.Persistent);
                ChunkNeighbourData.ChunkNeighbourDataValid[i] = false;
                continue;
            }
            neighbours[i].NeighbourDepecency(1);
            ChunkNeighbourData[i] = neighbours[i].GetBlocks();
            ChunkNeighbourData.ChunkNeighbourDataValid[i] = true;
        }
    }

    public void FreeNeighboursData()
    {
        for (int i = 0; i < 6; i++)
        {
            if (neighbours == null || neighbours[i] == null) continue;
            neighbours[i].NeighbourDepecency(-1);
        }
        ChunkNeighbourData.Destroy();
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

        try
        {
            blocksClusterIdDatas.Dispose();
            clusterBlockIdDatas.Dispose();
            clusterPositionDatas.Dispose();
            clusterSizeDatas.Dispose();
            clusterSidesVisibilityData.Dispose();
        }
        catch { }

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
            chunkPos = chunk.ChunkPosition,
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
            chunkPosition = chunk.ChunkPosition
        };

        generatingBlockIdJobHandle = generateBlockIdJob.Schedule(Size.x * Size.y * Size.z, 32);
    }

}
