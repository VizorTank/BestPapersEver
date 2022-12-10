using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class WorldClass : MonoBehaviour, IWorld
{
    // Public Values
    public string WorldName = "World1";
    public bool GenerateNewWorld = true;
    public int RenderDistance = 4;
    public RenderType RenderType = RenderType.GreedyMeshing;
    public WorldStaticData WorldData;
    public GameObject Player;


    // Private Values
    private ISaveManager _saveManager;
    private EntityManager _entityManager;
    private int3 playerLastChunkPos = new int3(0, 1, 0);
    private bool _showWorld = true;
    private Dictionary<int3, IChunk> activeChunksList = new Dictionary<int3, IChunk>();
    public Dictionary<int2, ChunkColumnData> ChunkColumnDatas = new Dictionary<int2, ChunkColumnData>();


    public string GetWorldName() => WorldName;
    public void SetWorldName(string name) => WorldName = name;
    public BlockTypesList GetBlockTypesList() => WorldData.BlockTypesList;
    public Transform GetTransform() => transform;
    public WorldBiomesList GetWorldBiomesList() => WorldData.WorldBiomes;


    public IChunk GetChunk(int3 chunkCoordinates)
    {
        if (!activeChunksList.ContainsKey(chunkCoordinates)) return null;
        return activeChunksList[chunkCoordinates];
    }

    public void RemoveChunk(int3 chunkCoordinates) => activeChunksList.Remove(chunkCoordinates);

    void Awake()
    {
        ChunkRendererConst.Init();
        _saveManager = SaveManager.GetInstance(this);
        if (!GenerateNewWorld)
            _saveManager.LoadWorld();
        
        _entityManager = new EntityManager(this);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        GetBlockTypesList().ProcessData();

        if (Player != null)
        {
            int3 pos = new int3((int)0.5f * VoxelData.ChunkSize.x, 0, (int)0.5f * VoxelData.ChunkSize.z);
            pos.y = GetChunkColumnData(new int3(0, 0, 0)).GetTerrainHeight(pos);
            Player.transform.position = new Vector3(pos.x + 0.5f, pos.y + 2, pos.z + 0.5f);
        }
    }

    private ChunkColumnData GetChunkColumnData(int3 chunkCoordinates)
    {
        int2 key = new int2(chunkCoordinates.x, chunkCoordinates.z);
        if (!ChunkColumnDatas.ContainsKey(key))
        {
            ChunkColumnDatas.Add(key, new ChunkColumnData(this, chunkCoordinates));
        }
        return ChunkColumnDatas[key];
    }

    public BiomeAttributesStruct GetBiome(int index) => WorldData.WorldBiomes.GetBiomeStruct(index);
    public ChunkGeneraionBiomes GetChunkGeneraionBiomes(int3 chunkCoordinates) => GetChunkColumnData(chunkCoordinates).ChunkGeneraionBiomes;
    public NativeArray<int> GetHeightMap(int3 chunkCoordinates) => GetChunkColumnData(chunkCoordinates).HeightMap;

    void Update()
    {
        Profiler.BeginSample("ShowWorld");
        ShowWorld();
        Profiler.EndSample();

        Profiler.BeginSample("LoadingChunks");
        _saveManager.Run();
        Profiler.EndSample();

        Profiler.BeginSample("DrawChunks");
        DrawChunks();
        Profiler.EndSample();

        Profiler.BeginSample("Spawn/Despawn Entities");
        _entityManager.Run();
        Profiler.EndSample();
    }

    private void CalculatePlayerPos(int3 playerChunkPos)
    {
        Profiler.BeginSample("Calculate player pos");
        int3 pPosDiff = playerChunkPos - playerLastChunkPos;
        _showWorld = math.any(pPosDiff != 0);
        Profiler.EndSample();
    }

    private void ReloadWorldAroundPlayer(int3 playerChunkPos)
    {
        var newActiveChunksList = new Dictionary<int3, IChunk>();
        int renderDistanceSize = RenderDistance * 2 + 1;
        for (int x = 0; x < renderDistanceSize; x++)
        {
            for (int z = 0; z < renderDistanceSize; z++)
            {
                for (int y = 0; y < WorldData.WorldHeightInChunks; y++)
                {
                    int3 pos = new int3(x - RenderDistance, y, z - RenderDistance) + playerChunkPos;
                    newActiveChunksList.Add(pos, GetOrCreateChunk(pos));
                    activeChunksList.Remove(pos);
                }
            }
        }
        _saveManager.UnloadChunks(this, activeChunksList);
        activeChunksList = newActiveChunksList;
    }
    
    private void ShowWorld()
    {
        int3 playerChunkPos = GetChunkCoords(Player.transform.position);
        playerChunkPos.y = 0;
        if (_showWorld || activeChunksList.Count <= 0)
        {
            ReloadWorldAroundPlayer(playerChunkPos);
            playerLastChunkPos = playerChunkPos;
        }

        CalculatePlayerPos(playerChunkPos);
    }

    public void DrawChunks()
    {
        int i = 0;
        foreach (var item in activeChunksList)
        {
            if (item.Value != null)
            {
                Profiler.BeginSample("Render");
                item.Value.Render();
                Profiler.EndSample();
            }
            else
                i++;
        }
        if (i > 0)
            Debug.LogWarning($"Missing {i} out of {activeChunksList.Count}");
    }

    void OnDestroy()
    {
        SaveWorld();
        GetBlockTypesList().Destroy();

        foreach (var item in ChunkColumnDatas)
        {
            item.Value.Destroy();
        }

        WorldData.WorldBiomes.Destroy();
        ChunkRendererConst.Destroy();
    }

    private void SaveWorld() => _saveManager.SaveWorld(this, activeChunksList);

    public IChunk GetOrCreateChunk(int3 chunkCoordinates)
    {
        if (GetChunk(chunkCoordinates) != null) return GetChunk(chunkCoordinates);
        return _saveManager.LoadChunk(this, chunkCoordinates);
    }

    public bool TryPlaceBlock(Vector3 position, int blockID)
    {
        int3 chunkPos = GetChunkCoords(position);
        if (GetChunk(chunkPos) == null) return false;
        if (!GetChunk(chunkPos).TryGetBlock(GetLocalPos(position), out int replacedBlockId)) return false;
        if (!GetBlockTypesList().areReplacable[replacedBlockId]) return false;
        return GetChunk(chunkPos).TrySetBlock(GetLocalPos(position), blockID, out int result);
    }

    public bool TrySetBlock(Vector3 position, int blockID, ref int replacedBlockId)
    {
        int3 chunkPos = GetChunkCoords(position);
        if (GetChunk(chunkPos) == null) return false;
        int3 p = GetLocalPos(position);
        bool result = GetChunk(chunkPos).TrySetBlock(p, blockID, out int retBlockId);
        replacedBlockId = retBlockId;
        return result;
    }

    public bool TryGetBlock(float3 position, ref int replacedBlockId)
    {
        int3 chunkPos = GetChunkCoords(position);
        if (GetChunk(chunkPos) == null) return false;
        bool tmp = GetChunk(chunkPos).TryGetBlock(GetLocalPos(position), out int result);
        replacedBlockId = result;
        return tmp;
    }

    public void CreateStructure(int3 position, int structureId)
    {
        CreateStructure(GetChunkCoords(position), GetLocalPos(position), structureId);
    }

    public void CreateStructure(int3 chunkCoordinates, int3 structurePos, int structureId)
    {
        IChunk chunk = GetOrCreateChunk(chunkCoordinates);
        chunk.CreateStructure(structurePos, structureId);
    }

    private int3 GetLocalPos(float3 position) => GetLocalPos((int3)math.floor(position));

    private int3 GetLocalPos(int3 position) => (position % VoxelData.ChunkSize + VoxelData.ChunkSize) % VoxelData.ChunkSize;

    public int GetBlock(Vector3 position)
    {
        int3 chunkPos = GetChunkCoords(position);
        GetChunk(chunkPos).TryGetBlock(GetLocalPos(position), out int result);
        return result;
    }

    private int3 GetChunkCoords(float3 position) => (int3)math.floor(position / VoxelData.ChunkSize);

    public Structure GetStructure(int structureId) => WorldData.Structures[structureId];
    public Vector3 GetPlayerPosition() => Player.transform.position;
    public RenderType GetRenderType() => RenderType;

    public void SetRenderDistance(int renderDistance)
    {   
        this.RenderDistance = renderDistance;
        _showWorld = true;
    }
    public int GetRenderDistance() => RenderDistance;

    public void SetRenderType(RenderType type)
    {
        if (type == RenderType) return;

        RenderType = type;

        _saveManager.UnloadChunks(this, activeChunksList);
        activeChunksList = new Dictionary<int3, IChunk>();
        _showWorld = true;
    }
    public float GetEnemiesDensity()
    {
        return 1;
    }

    public int GetWorldHeightInChunks()
    {
        return WorldData.WorldHeightInChunks;
    }
}

public enum RenderType
{
    GreedyMeshing,
    Instancing
}

