using UnityEngine;

public class ChunkRenderer : IChunkRenderer
{
    private WorldClass _world;
    
    private Chunk _chunk;
    
    private GameObject _chunkObject;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    
    private ChunkRendererStateMachine _stateMachine;

    private bool _requireUpdate = true;

    public ChunkRenderer(WorldClass world, Chunk chunk)
    {
        _world = world;
        SetChunk(chunk);
        _stateMachine = new ChunkRendererStateMachine();
    }

    private void SetChunk(Chunk chunk)
    {
        _chunk = chunk;
        
        _chunkObject = new GameObject();
        _meshFilter = _chunkObject.AddComponent<MeshFilter>();
        _meshRenderer = _chunkObject.AddComponent<MeshRenderer>();
        
        _meshRenderer.materials = _world.Materials.ToArray();
        
        _chunkObject.transform.SetParent(_world.transform);
        _chunkObject.transform.position = Vector3.Scale(
            new Vector3(chunk.Coordinates.x, chunk.Coordinates.y, chunk.Coordinates.z), 
            new Vector3(VoxelData.ChunkSize.x, VoxelData.ChunkSize.y, VoxelData.ChunkSize.z));
        _chunkObject.name = string.Format("Chunk {0}, {1}, {2}", chunk.Coordinates.x, chunk.Coordinates.y, chunk.Coordinates.z);
    }
    
    public void Render()
    {
        if (_requireUpdate)
        {
            _stateMachine.Init(_chunk, _world);
            _stateMachine.CopyBlocks();
            _stateMachine.CreateClusters();
            _stateMachine.CheckClusterVisibility();
            _stateMachine.CreateMeshDataWithClusters();
            if (_stateMachine.CreateMesh(out Mesh mesh))
            {
                _meshFilter.mesh = mesh;
                //Debug.Log("Created Mesh");
                _requireUpdate = false;
            }
        }
    }

    public void Unload()
    {
        
    }

    public void Update()
    {
        _requireUpdate = true;
    }
}