using Unity.Mathematics;
using UnityEngine;

public class ChunkRenderer : IChunkRenderer
{
    private WorldClass _world;
    
    private IChunk _chunk;
    
    private GameObject _chunkObject;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    
    private ChunkRendererStateMachine _stateMachine;

    private bool _requireUpdate;

    public ChunkRenderer(IChunk chunk, WorldClass world)
    {
        _world = world;
        SetChunk(chunk);
        _stateMachine = new ChunkRendererStateMachine();
    }

    public void Destroy()
    {
        _stateMachine.Destroy();
    }

    private void SetChunk(IChunk chunk)
    {
        _chunk = chunk;
        
        _chunkObject = new GameObject();
        _meshFilter = _chunkObject.AddComponent<MeshFilter>();
        _meshRenderer = _chunkObject.AddComponent<MeshRenderer>();
        
        _meshRenderer.materials = _world.Materials.ToArray();
        
        int3 chunkCoordinates = chunk.GetChunkCoordinates();

        _chunkObject.transform.SetParent(_world.transform);
        _chunkObject.transform.position = Vector3.Scale(
            new Vector3(chunkCoordinates.x, chunkCoordinates.y, chunkCoordinates.z), 
            new Vector3(VoxelData.ChunkSize.x, VoxelData.ChunkSize.y, VoxelData.ChunkSize.z));
        _chunkObject.name = string.Format("Chunk {0}, {1}, {2}", chunkCoordinates.x, chunkCoordinates.y, chunkCoordinates.z);
    }
    
    public void Render()
    {
        if (_requireUpdate && _stateMachine.Init(_chunk, _world))
        {
            _requireUpdate = false;
        }
        _stateMachine.CopyBlocks();
        _stateMachine.CreateClusters();
        _stateMachine.CheckClusterVisibility();
        _stateMachine.CreateMeshDataWithClusters();
        if (_stateMachine.CreateMesh(out Mesh mesh))
        {
            _meshFilter.mesh = mesh;
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