using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class ChunkRenderer : IChunkRenderer
{
    private IWorld _world;
    private IChunk _chunk;
    
    private GameObject _chunkObject;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    
    private ChunkRendererStateMachine _stateMachine;

    private bool _requireUpdate;
    private bool _processing;

    public ChunkRenderer(IChunk chunk, IWorld world)
    {
        _world = world;
        SetChunk(chunk);
        _stateMachine = new ChunkRendererStateMachine();
    }

    public void Destroy()
    {
        _stateMachine.Destroy();
        MonoBehaviour.Destroy(_chunkObject);
    }

    private void SetChunk(IChunk chunk)
    {
        _chunk = chunk;
        
        _chunkObject = new GameObject();
        _meshFilter = _chunkObject.AddComponent<MeshFilter>();
        _meshRenderer = _chunkObject.AddComponent<MeshRenderer>();
        
        _meshRenderer.materials = _world.GetBlockTypesList().Materials.ToArray();
        
        int3 chunkCoordinates = chunk.GetChunkCoordinates();

        _chunkObject.transform.SetParent(_world.GetTransform());
        _chunkObject.transform.position = Vector3.Scale(
            new Vector3(chunkCoordinates.x, chunkCoordinates.y, chunkCoordinates.z), 
            new Vector3(VoxelData.ChunkSize.x, VoxelData.ChunkSize.y, VoxelData.ChunkSize.z));
        _chunkObject.name = string.Format("Chunk {0}, {1}, {2}", chunkCoordinates.x, chunkCoordinates.y, chunkCoordinates.z);
    }
    
    public void Render(ChunkNeighbours neighbours)
    {
        if (_requireUpdate && _stateMachine.Init(_chunk, _world))
        {
            _requireUpdate = false;
            _processing = true;
        }
        _stateMachine.CopyBlocks();
        _stateMachine.CreateClusters();
        _stateMachine.CheckClusterVisibility(neighbours);
        _stateMachine.CreateMeshDataWithClusters(neighbours);
        if (_stateMachine.CreateMesh(out Mesh mesh))
        {
            if (_meshFilter == null)
                Debug.Log("Missing Mesh Filter");
            _meshFilter.mesh = mesh;
            _processing = false;
        }
    }

    public void Update() => _requireUpdate = true;

    public bool RequireProcessing() => _requireUpdate || _processing;

    public ComputeBuffer GetBlocksBuffer() => ChunkRendererConst.voidChunkBlockIdBuffer;
}