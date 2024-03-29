using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkRendererStateMachine
{
    private IChunk _chunk;
    private IWorld _world;

    private ChunkRendererStates _state = ChunkRendererStates.RequireInit;
    private NativeArray<int> blocksForMeshGeneration;
    
    private NativeList<ClusterCreationStruct> ClusterData;
    private NativeList<ClusterSidesDataStruct> ClusterSidesData;
    private NativeArray<int> blocksClusterIdDatas;
    
    private JobHandle generatingClustersJobHandle;
    private JobHandle checkingVisibilityJobHandle;
    private JobHandle generatingMeshJobHandle;
    
    private Mesh.MeshDataArray meshDataArray;

    private bool _isDestroyed = false;

    public ChunkRendererStateMachine()
    {
        ClusterData = new NativeList<ClusterCreationStruct>(Allocator.Persistent);

        ClusterSidesData = new NativeList<ClusterSidesDataStruct>(
            VoxelData.ChunkSize.x * VoxelData.ChunkSize.y * VoxelData.ChunkSize.z * 6, 
            Allocator.Persistent
        );

        blocksForMeshGeneration = new NativeArray<int>(
            VoxelData.ChunkSize.x * VoxelData.ChunkSize.y * VoxelData.ChunkSize.z, 
            Allocator.Persistent);
        
        blocksClusterIdDatas = new NativeArray<int>(
            VoxelData.ChunkSize.x * VoxelData.ChunkSize.y * VoxelData.ChunkSize.z, 
            Allocator.Persistent);
    }
    
    public void Destroy()
    {
        _isDestroyed = true;
        try { ClusterData.Dispose(); } catch { }
        try { ClusterSidesData.Dispose(); } catch { }
        try { blocksForMeshGeneration.Dispose(); } catch { }
        try { blocksClusterIdDatas.Dispose(); } catch { }
    }

    public bool Init(IChunk chunk, IWorld world)
    {
        if (_isDestroyed) return false;
        if (_state != ChunkRendererStates.RequireInit) return false;
        _state = ChunkRendererStates.Ready;

        _chunk = chunk;
        _world = world;

        ClusterData.Clear();
        return true;
    }

    public void CopyBlocks()
    {
        if (_state != ChunkRendererStates.Ready) return;
        
        _state = ChunkRendererStates.CopyingBlocks;
        _chunk.GetBlocks().CopyTo(blocksForMeshGeneration);
    }
    
    public void CreateClusters()
    {
        if (_state != ChunkRendererStates.CopyingBlocks) return;
        _state = ChunkRendererStates.CreatingClusters;
        ClusterData.Clear();
        
        CreateClustersJob createClustersJob = new CreateClustersJob
        {
            blockIdDatas = blocksForMeshGeneration,

            blocksClusterIdDatas = blocksClusterIdDatas,
            
            Clusters = ClusterData,

            axis = ChunkRendererConst.axisArray,
            chunkSize = VoxelData.ChunkSize
        };
        generatingClustersJobHandle = createClustersJob.Schedule();
    }
    
    public void CheckClusterVisibility(ChunkNeighbours neighbours)
    {
        if (_state != ChunkRendererStates.CreatingClusters || !generatingClustersJobHandle.IsCompleted) return;
        ChunkNeighbourData data = neighbours.GetData();
        _state = ChunkRendererStates.CheckingVisibility;

        generatingClustersJobHandle.Complete();

        ClusterSidesData.Clear();
        NativeList<ClusterSidesDataStruct>.ParallelWriter writer = ClusterSidesData.AsParallelWriter();
        
        CheckClusterVisibilityJob checkClusterVisibilityJob = new CheckClusterVisibilityJob
        {
            blockIdDatas = blocksForMeshGeneration,
            
            ClusterData = ClusterData,

            blockTypesIsTransparent = _world.GetBlockTypesList().areTransparent,

            chunkNeighbourData = data,

            neighbours = ChunkRendererConst.voxelNeighbours,
            axis = ChunkRendererConst.axisArray,
            clusterSides = ChunkRendererConst.clusterSides,
            chunkSize = VoxelData.ChunkSize,

            Writer = writer
        };
        checkingVisibilityJobHandle = checkClusterVisibilityJob.Schedule(ClusterData.Length, 16);
    }

    public void CreateMeshDataWithClusters(ChunkNeighbours neighbours)
    {
        if (_state != ChunkRendererStates.CheckingVisibility || !checkingVisibilityJobHandle.IsCompleted) return;
        
        _state = ChunkRendererStates.CreatingMeshData;

        checkingVisibilityJobHandle.Complete();
        neighbours.ReleaseData();
        
        // Allocate mesh to create
        meshDataArray = Mesh.AllocateWritableMeshData(1);

        CreateMeshWithClustersJob createMeshWithClustersJob = new CreateMeshWithClustersJob
        {
            ClusterSidesData = ClusterSidesData,
            
            // Const
            neighbours = ChunkRendererConst.voxelNeighbours,
            voxelTris = ChunkRendererConst.voxelTris,
            voxelTrisSize = ChunkRendererConst.voxelTrisSize,
            voxelUvs = ChunkRendererConst.voxelUvs,
            voxelVerts = ChunkRendererConst.voxelVerts,

            triangleOrder = ChunkRendererConst.triangleOrder,

            // Block Types Count
            blockTypesCount = _world.GetBlockTypesList().blockTypes.Count,
            blockTypesIsInvisible = _world.GetBlockTypesList().areInvisible,

            // How data is inserted to MeshData
            layout = ChunkRendererConst.layout,

            // Chunk position
            chunkPos = _chunk.GetChunkPosition(),
            chunkSize = VoxelData.ChunkSize,

            // Mesh to create
            data = meshDataArray[0]
        };

        // Schedule job
        generatingMeshJobHandle = createMeshWithClustersJob.Schedule();
    }

    public bool CreateMesh(out Mesh mesh)
    {
        if (_state != ChunkRendererStates.CreatingMeshData || 
            !generatingMeshJobHandle.IsCompleted)
        {
            mesh = null;
            return false;
        }
        _state = ChunkRendererStates.RequireInit;

        generatingMeshJobHandle.Complete();

        mesh = new Mesh();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return true;
    }
}

public enum ChunkRendererStates
{
    RequireInit,
    Ready,
    CopyingBlocks,
    CreatingClusters,
    CheckingVisibility,
    CreatingMeshData
}

public static class ChunkRendererConst
{
    public static NativeArray<int3> axisArray;
    
    public static NativeArray<int3> voxelNeighbours;
    public static NativeArray<float3> voxelVerts;
    public static int voxelTrisSize;
    public static NativeArray<int> voxelTris;
    public static NativeArray<float2> voxelUvs;
    public static NativeArray<int> triangleOrder;
    
    public static NativeArray<VertexAttributeDescriptor> layout;
 
    public static NativeArray<int3> clusterSides;
    public static NativeArray<int> voidChunkBlockId;
    public static ComputeBuffer voidChunkBlockIdBuffer;

    public static void Init()
    {
        axisArray = new NativeArray<int3>(VoxelData.axisArray, Allocator.Persistent);
        voxelNeighbours = new NativeArray<int3>(VoxelData.voxelNeighbours, Allocator.Persistent);
        voxelVerts = new NativeArray<float3>(VoxelData.voxelVerts, Allocator.Persistent);
        voxelTrisSize = VoxelData.voxelTrisSize;
        voxelTris = new NativeArray<int>(VoxelData.voxelTris, Allocator.Persistent);
        voxelUvs = new NativeArray<float2>(VoxelData.voxelUvs, Allocator.Persistent);
        triangleOrder = new NativeArray<int>(VoxelData.triangleOrder, Allocator.Persistent);
        layout = new NativeArray<VertexAttributeDescriptor>(VoxelData.layoutVertex, Allocator.Persistent);
        clusterSides = new NativeArray<int3>(VoxelData.clusterSidesArray, Allocator.Persistent);
        voidChunkBlockId = new NativeArray<int>(VoxelData.GetVolume(), Allocator.Persistent);

        voidChunkBlockIdBuffer = new ComputeBuffer(VoxelData.GetVolume(), sizeof(int));
    }

    public static void Destroy()
    {
        try { axisArray.Dispose(); } catch { }

        try { voxelNeighbours.Dispose(); } catch { }
        try { voxelVerts.Dispose(); } catch { }
        try { voxelTris.Dispose(); } catch { }
        try { voxelUvs.Dispose(); } catch { }
        try { triangleOrder.Dispose(); } catch { }
        try { layout.Dispose(); } catch { }

        try { clusterSides.Dispose(); } catch { }

        try { voidChunkBlockId.Dispose(); } catch { }
        try { voidChunkBlockIdBuffer.Dispose(); } catch { }
    }
} 