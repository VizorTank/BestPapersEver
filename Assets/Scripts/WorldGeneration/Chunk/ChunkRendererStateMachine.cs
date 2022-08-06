using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkRendererStateMachine
{
    public Chunk Chunk;
    public WorldClass World;

    private ChunkRendererStates _state = ChunkRendererStates.Ready;
    private NativeArray<int> blocksForMeshGeneration;
    
    private NativeList<ClusterCreationStruct> ClusterData;
    private NativeArray<int> blocksClusterIdDatas;
    
    private JobHandle generatingBlockIdJobHandle;
    private JobHandle generatingClustersJobHandle;
    private JobHandle checkingVisibilityJobHandle;
    private JobHandle generatingMeshJobHandle;
    
    private Mesh.MeshDataArray meshDataArray;
    private ChunkNeighbourData ChunkNeighbourData;
    
    public void Init(Chunk chunk, WorldClass world)
    {
        if (Chunk != null) return;
        Chunk = chunk;
        World = world;

        ClusterData = new NativeList<ClusterCreationStruct>(Allocator.Persistent);

        blocksForMeshGeneration = new NativeArray<int>(
            VoxelData.ChunkSize.x * VoxelData.ChunkSize.y * VoxelData.ChunkSize.z, 
            Allocator.Persistent);
        
        blocksClusterIdDatas = new NativeArray<int>(
            VoxelData.ChunkSize.x * VoxelData.ChunkSize.y * VoxelData.ChunkSize.z, 
            Allocator.Persistent);
    }

    public void CopyBlocks()
    {
        if (_state != ChunkRendererStates.Ready) return;

        _state = ChunkRendererStates.CopyingBlocks;

        Chunk.GetBlocks().CopyTo(blocksForMeshGeneration);
    }
    
    public void CreateClusters()
    {

        if (_state != ChunkRendererStates.CopyingBlocks || !generatingBlockIdJobHandle.IsCompleted) return;
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
    

    private void GetNeighboursData()
    {
        ChunkNeighbourData = Chunk.GetNeighbourData();
    }

    private void FreeNeighboursData()
    {
        Chunk.FreeNeighboursData();
    }
    
    public void CheckClusterVisibility()
    {
        
        if (_state != ChunkRendererStates.CreatingClusters || !generatingClustersJobHandle.IsCompleted) return;
        _state = ChunkRendererStates.CheckingVisibility;

        generatingClustersJobHandle.Complete();
        GetNeighboursData();

        CheckClusterVisibilityJob checkClusterVisibilityJob = new CheckClusterVisibilityJob
        {
            blockIdDatas = blocksForMeshGeneration,
            
            ClusterData = ClusterData,

            blockTypesIsTransparent = World.blockTypesList.areTransparent,

            chunkNeighbourData = ChunkNeighbourData,

            neighbours = ChunkRendererConst.voxelNeighbours,
            clusterSides = ChunkRendererConst.clusterSides,
            chunkSize = VoxelData.ChunkSize
        };
        checkingVisibilityJobHandle = checkClusterVisibilityJob.Schedule(ClusterData.Length, 16);
    }

    public void CreateMeshDataWithClusters()
    {
        if (_state != ChunkRendererStates.CheckingVisibility || !checkingVisibilityJobHandle.IsCompleted) return;
        _state = ChunkRendererStates.CreatingMeshData;

        checkingVisibilityJobHandle.Complete();
        FreeNeighboursData();

        // Allocate mesh to create
        meshDataArray = Mesh.AllocateWritableMeshData(1);

        CreateMeshWithClustersJob createMeshWithClustersJob = new CreateMeshWithClustersJob
        {
            ClusterData = ClusterData,
            
            // Const
            neighbours = ChunkRendererConst.voxelNeighbours,
            voxelTris = ChunkRendererConst.voxelTris,
            voxelTrisSize = ChunkRendererConst.voxelTrisSize,
            voxelUvs = ChunkRendererConst.voxelUvs,
            voxelVerts = ChunkRendererConst.voxelVerts,

            triangleOrder = ChunkRendererConst.triangleOrder,

            // Block Types Count
            blockTypesCount = World.blockTypesList.BlockCount,
            blockTypesIsInvisible = World.blockTypesList.areInvisible,

            // How data is inserted to MeshData
            layout = ChunkRendererConst.layout,

            // Chunk position
            chunkPos = Chunk.ChunkPosition,
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
        _state = ChunkRendererStates.Ready;

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
    Ready,
    CopyingBlocks,
    CreatingClusters,
    CheckingVisibility,
    CreatingMeshData,
    CreatingMesh
}

public static class ChunkRendererConst
{
    public static NativeArray<int3> axisArray = new NativeArray<int3>(VoxelData.axisArray, Allocator.Persistent);
    
    public static NativeArray<int3> voxelNeighbours = new NativeArray<int3>(VoxelData.voxelNeighbours, Allocator.Persistent);
    public static NativeArray<float3> voxelVerts = new NativeArray<float3>(VoxelData.voxelVerts, Allocator.Persistent);
    public static int voxelTrisSize = VoxelData.voxelTrisSize;
    public static NativeArray<int> voxelTris = new NativeArray<int>(VoxelData.voxelTris, Allocator.Persistent);
    public static NativeArray<float2> voxelUvs = new NativeArray<float2>(VoxelData.voxelUvs, Allocator.Persistent);
    public static NativeArray<int> triangleOrder = new NativeArray<int>(VoxelData.triangleOrder, Allocator.Persistent);
    
    public static NativeArray<VertexAttributeDescriptor> layout = new NativeArray<VertexAttributeDescriptor>(VoxelData.layoutVertex, Allocator.Persistent);
 
    public static NativeArray<int3> clusterSides = new NativeArray<int3>(VoxelData.clusterSidesArray, Allocator.Persistent);

    public static void Destroy()
    {
        try { axisArray.Dispose(); } catch { }

        try { voxelNeighbours.Dispose(); } catch { }
        try { voxelVerts.Dispose(); } catch { }
        try { voxelUvs.Dispose(); } catch { }
        try { voxelTris.Dispose(); } catch { }
        try { triangleOrder.Dispose(); } catch { }
        try { layout.Dispose(); } catch { }

        try { clusterSides.Dispose(); } catch { }
    }
} 