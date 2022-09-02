using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MyWorld : MonoBehaviour, IWorld
{
    public GameObject Player;

    public int WorldSizeInChunks = 64;
    public int WorldHeightInChunks = 16;
    public int RenderDistance = 4;
    //private IChunk[,,] _chunks;
    private Dictionary<int3, IChunk> _activeChunks = new Dictionary<int3, IChunk>();
    private Dictionary<int3, IChunk> _loadedChunks = new Dictionary<int3, IChunk>();
    private Dictionary<int3, IChunk> _chunksToUnload = new Dictionary<int3, IChunk>();

    private ISaveManager _saveManager;

    private void Start()
    {
        _saveManager = MySaveManager.GetInstance();
        //_chunks = new Chunk[WorldSizeInChunks, WorldHeightInChunks, WorldSizeInChunks];
    }

    private void Update()
    {
        if (DidPlayerMoved())
        {
            LoadChunks();
        }
        DrawChunks();
    }

    public int3 playerLastChunkPos = new int3();

    private bool DidPlayerMoved()
    {
        int3 playerChunkPos = VoxelData.GetChunkCoordinates(Player.transform.position);
        playerChunkPos.y = 0;
        int3 pPosDiff = playerChunkPos - playerLastChunkPos;
        return math.any(pPosDiff != 0);
    }

    public void CreateNewChunk()
    {
        throw new System.NotImplementedException();
    }

    public void DrawChunks()
    {
        throw new System.NotImplementedException();
    }

    // Loads new chunks when player moves
    private void LoadChunks()
    {
        _chunksToUnload = _activeChunks;
        _activeChunks = new Dictionary<int3, IChunk>();
        for (int x = 0; x < RenderDistance; x++)
        {
            for (int z = 0; z < RenderDistance; z++)
            {
                for (int y = 0; y < WorldHeightInChunks; y++)
                {
                    LoadActiveChunk(new int3(x, y, z));
                }
            }
        }
    }

    private void LoadActiveChunk(int3 chunkCoordinates)
    {
        if (_chunksToUnload.ContainsKey(chunkCoordinates))
        {
            _activeChunks.Add(chunkCoordinates, _chunksToUnload[chunkCoordinates]);
            _chunksToUnload.Remove(chunkCoordinates);
            return;
        }
        else
        {
            IChunk chunk = _saveManager.LoadChunk(this, chunkCoordinates);
            _activeChunks.Add(chunkCoordinates, chunk);
        }

    }

    public void GetBiome()
    {
        throw new System.NotImplementedException();
    }

    public void GetNeighbours()
    {
        throw new System.NotImplementedException();
    }

    public void IsInWorld()
    {
        throw new System.NotImplementedException();
    }

    public void SetChunkActive()
    {
        throw new System.NotImplementedException();
    }

    public void TryGetBlock()
    {
        throw new System.NotImplementedException();
    }

    public void TryGetGlocks()
    {
        throw new System.NotImplementedException();
    }

    public void TryPlaceBlock()
    {
        throw new System.NotImplementedException();
    }

    public void TrySetBlock()
    {
        throw new System.NotImplementedException();
    }
}