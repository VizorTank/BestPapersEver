using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class WorldClass : MonoBehaviour, IWorld
{
    public string WorldName = "World1";

    public bool GenerateNewWorld = true;
    public RenderType RenderType = RenderType.GreedyMeshing;
    public WorldStaticData WorldData;
    public GameObject Player;
    private Dictionary<int3, IChunk> activeChunksList = new Dictionary<int3, IChunk>();
    private ISaveManager _saveManager;
    private int3 playerLastChunkPos = new int3(0, 1, 0);
    private bool _showWorld = true;
    public Dictionary<int2, ChunkColumnData> ChunkColumnDatas = new Dictionary<int2, ChunkColumnData>();

    public int RenderDistance = 16;
    

    public static int WorldCubeSize = 64;
    public int3 WorldSizeInChunks = new int3(WorldCubeSize, 2, WorldCubeSize);
    private static int3 ChunkSize { get => VoxelData.ChunkSize; }

    public BlockTypesList blockTypesList;

    public List<Structure> Structures = new List<Structure>();

    public WorldBiomesList WorldBiomes;


    public List<Material> Materials
    {
        get => blockTypesList.Materials;
    }


    public string GetWorldName()
    {
        return WorldName;
    }

    public void SetWorldName(string name)
    {
        WorldName = name;
    }

    public BlockTypesList GetBlockTypesList()
    {
        return blockTypesList;
    }

    public Transform GetTransform()
    {
        return transform;
    }
    
    public WorldBiomesList GetWorldBiomesList()
    {
        return WorldBiomes;
    }

    public IChunk GetChunk(int3 chunkCoordinates)
    {
        Profiler.BeginSample("Contains chunk");
        bool contains = activeChunksList.ContainsKey(chunkCoordinates);
        Profiler.EndSample();
        if (!contains) return null;
        return activeChunksList[chunkCoordinates];
    }

    public void RemoveChunk(int3 chunkCoordinates)
    {
        bool contains = activeChunksList.ContainsKey(chunkCoordinates);
        if (!contains) return;
        activeChunksList.Remove(chunkCoordinates);
    }

    // Start is called before the first frame update
    void Awake()
    {
        ChunkRendererConst.Init();
        _saveManager = SaveManager.GetInstance(this);
        if (!GenerateNewWorld)
            _saveManager.LoadWorld();
    }

    void Start()
    {
        // Cursor.lockState = CursorLockMode.Locked;
        
        blockTypesList.ProcessData();

        CreateTreeStructure();


        if (Player != null)
        {
            Player.transform.position = new Vector3(0.5f, 
                VoxelData.ChunkSize.y * WorldSizeInChunks.y, 
                0.5f);
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

    public BiomeAttributesStruct GetBiome(int index)
    {
        return WorldBiomes.GetBiomeStruct(index);
    }

    public int GetBiomeIndex(int3 chunkCoordinates)
    {
        // return WorldBiomes.GetBiomeIndex(chunkCoordinates);
        return GetChunkColumnData(chunkCoordinates).BiomeIndex;
    }

    public ChunkGeneraionBiomes GetChunkGeneraionBiomes(int3 chunkCoordinates)
    {
        // return WorldBiomes.GetChunkGeneraionBiomes(this, chunkCoordinates);
        return GetChunkColumnData(chunkCoordinates).ChunkGeneraionBiomes;
    }

    public NativeArray<int> GetHeightMap(int3 chunkCoordinates)
    {
        return GetChunkColumnData(chunkCoordinates).HeightMap;
    }

    public void CreateTreeStructure()
    {
        Structure structure = new Structure(new Vector3Int(5, 7, 5));
        structure.BlockId = 1;

        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 5; z++)
                {
                    structure.Blocks[x][2 + y][z] = 9;
                }
            }
        }

        structure.Blocks[2][0][2] = 8;
        structure.Blocks[2][1][2] = 8;
        structure.Blocks[2][2][2] = 8;
        structure.Blocks[2][3][2] = 8;
        structure.Blocks[2][4][2] = 8;

        structure.Blocks[0][2][0] = 0;
        structure.Blocks[0][3][0] = 0;

        structure.Blocks[4][2][0] = 0;
        structure.Blocks[4][3][0] = 0;

        structure.Blocks[0][2][4] = 0;
        structure.Blocks[0][3][4] = 0;

        structure.Blocks[4][2][4] = 0;
        structure.Blocks[4][3][4] = 0;

        structure.Blocks[1][4][2] = 9;
        structure.Blocks[3][4][2] = 9;
        structure.Blocks[2][4][1] = 9;
        structure.Blocks[2][4][3] = 9;

        structure.Blocks[1][5][2] = 9;
        structure.Blocks[3][5][2] = 9;
        structure.Blocks[2][5][1] = 9;
        structure.Blocks[2][5][3] = 9;

        structure.Blocks[2][5][2] = 9;


        structure.Hitbox = new Vector3Int(3, 6, 3);
        structure.HitboxOffset = new Vector3Int(1, 1, 1);

        Structures.Add(structure);
    }


    // Update is called once per frame
    void Update()
    {
        Profiler.BeginSample("ShowWorld");
        ShowWorld();
        Profiler.EndSample();

        Profiler.BeginSample("LoadingChunks");
        // _saveManager.LoadingChunks();
        _saveManager.Run();
        Profiler.EndSample();

        Profiler.BeginSample("DrawChunks");
        DrawChunks();
        Profiler.EndSample();
        // Debug.Log(activeChunksList.Count);
    }

    private void CalculatePlayerPos(int3 playerChunkPos)
    {
        Profiler.BeginSample("Calculate player pos");
        playerChunkPos.y = 0;
        int3 pPosDiff = playerChunkPos - playerLastChunkPos;
        _showWorld = math.any(pPosDiff != 0);
        Profiler.EndSample();
    }
    
    private void ShowWorld()
    {
        int3 playerChunkPos = GetChunkCoords(Player.transform.position);
        if (_showWorld)
        {
            var newActiveChunksList = new Dictionary<int3, IChunk>();
            int renderDistanceSize = RenderDistance * 2 + 1;
            // int renderDistanceSize = renderDistance;
            for (int x = 0; x < renderDistanceSize; x++)
            {
                for (int z = 0; z < renderDistanceSize; z++)
                {
                    for (int y = -1; y < WorldSizeInChunks.y; y++)
                    {
                        int3 pos = new int3(x - RenderDistance, y, z - RenderDistance) + playerChunkPos;
                        newActiveChunksList.Add(pos, GetOrCreateChunk(pos));
                        if (activeChunksList.ContainsKey(pos))
                            activeChunksList.Remove(pos);
                    }
                }
            }
            _saveManager.UnloadChunks(this, activeChunksList);
            activeChunksList = newActiveChunksList;
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
            //     Debug.LogWarning("Missing Chunk");
        }
        if (i > 0)
            Debug.LogWarning($"Missing {i} out of {activeChunksList.Count}");
    }

    void OnDestroy()
    {
        SaveWorld();
        blockTypesList.Destroy();

        foreach (var item in ChunkColumnDatas)
        {
            item.Value.Destroy();
        }
        // foreach (var biome in BiomeAttributesStruct)
        // {
        //     biome.lodes.Dispose();
        // }
        WorldBiomes.Destroy();
        // BiomeAttributesStruct.lodes.Dispose();
        SaveManager.Destroy();
        ChunkRendererConst.Destroy();
    }

    private void SaveWorld()
    {
        _saveManager.UnloadChunks(this, activeChunksList);
        // _saveManager.WaitForChunksToSave();
        _saveManager.SaveWorld();
    }

    public bool IsCoordInWorld(int3 chunkCoordinates)
    {
        return true;
        // if (coordinates.x < 0 || coordinates.x >= WorldSizeInChunks.x ||
        //     coordinates.y < -1 || coordinates.y >= WorldSizeInChunks.y ||
        //     coordinates.z < 0 || coordinates.z >= WorldSizeInChunks.z) return false;
        // return true;
    }

    public IChunk GetOrCreateChunk(int3 chunkCoordinates)
    {
        // TODO: Center of World in (0, 0)
        if (!IsCoordInWorld(chunkCoordinates) || GetChunk(chunkCoordinates) != null) return GetChunk(chunkCoordinates);
        return _saveManager.LoadChunk(this, chunkCoordinates);
    }

    public bool TryPlaceBlock(Vector3 position, int blockID)
    {
        // if (!IsInWorld(position)) return false;
        int3 chunkPos = GetChunkCoords(position);
        if (GetChunk(chunkPos) == null) return false;
        if (!GetChunk(chunkPos).TryGetBlock(GetLocalPos(position), out int replacedBlockId)) return false;
        if (!blockTypesList.areReplacable[replacedBlockId]) return false;
        return GetChunk(chunkPos).TrySetBlock(GetLocalPos(position), blockID, out int result);
    }

    public bool TrySetBlock(Vector3 position, int blockID, ref int replacedBlockId)
    {
        // if (!IsInWorld(position)) return false;
        int3 chunkPos = GetChunkCoords(position);
        if (GetChunk(chunkPos) == null) return false;
        int3 p = GetLocalPos(position);
        bool result = GetChunk(chunkPos).TrySetBlock(p, blockID, out int retBlockId);
        replacedBlockId = retBlockId;
        // Debug.Log($"Placed block: {p.x}, {p.y}, {p.z} Pos: {position.x}, {position.y}, {position.z}");
        return result;
    }

    public bool TryGetBlock(float3 position, ref int replacedBlockId)
    {
        // if (!IsInWorld(position)) return false;
        int3 chunkPos = GetChunkCoords(position);
        if (GetChunk(chunkPos) == null) return false;
        bool tmp = GetChunk(chunkPos).TryGetBlock(GetLocalPos(position), out int result);
        replacedBlockId = result;
        return tmp;
    }

    // public int SetBlock(Vector3 position, int blockID)
    // {
    //     int3 chunkPos = GetChunkCoords(position);
    //     GetChunk(chunkPos).TrySetBlock(new int3(position) % ChunkSize, blockID, out int result);
    //     return result;
    // }

    public void CreateStructure(Vector3 position, int structureId) => CreateStructure(new int3(position), structureId);
    // public void CreateStructure(int3 position, int structureId) => CreateStructure(position, GetStructure(structureId));
    public void CreateStructure(int3 position, int structureId)
    {
        // // Debug.Log("Created structure "  + position.x + " " + position.y + " " + position.z);
        // int3 chunkPos = GetChunkCoords(position);
        // // if (!IsInWorld(chunkPos)) return;
        // // Debug.Log("Created structure2");
        // IChunk chunk = GetOrCreateChunk(chunkPos);
        // chunk.CreateStructure(GetLocalPos(position), structureId);
        CreateStructure(GetChunkCoords(position), GetLocalPos(position), structureId);
    }

    public void CreateStructure(int3 chunkCoordinates, int3 structurePos, int structureId)
    {
        // if (!IsInWorld(chunkCoordinates)) return;
        // Debug.Log("Created structure2");
        IChunk chunk = GetOrCreateChunk(chunkCoordinates);
        chunk.CreateStructure(structurePos, structureId);
    }

    private int3 GetLocalPos(float3 position)
    {
        int3 p = new int3(math.floor(position));
        return GetLocalPos(new int3(p));
    }
    private int3 GetLocalPos(int3 position)
    {
        return (position % ChunkSize + ChunkSize) % ChunkSize;
    }

    public int GetBlock(Vector3 position)
    {
        int3 chunkPos = GetChunkCoords(position);
        GetChunk(chunkPos).TryGetBlock(GetLocalPos(position), out int result);
        return result;
    }

    private int3 GetChunkCoords(float3 position)
    {
        int3 cPos = (int3)math.floor(position / ChunkSize);
        return cPos;
    }

    public ChunkNeighbours GetNeighbours(int3 chunkCoordinates)
    {
        ChunkNeighbours neighbours = new ChunkNeighbours();
        for (int i = 0; i < 6; i++)
        {
            if (neighbours[i] != null && !neighbours[i].IsDestroyed()) continue;

            int3 neighbourCoordinates = chunkCoordinates + VoxelData.voxelNeighbours[i];
            Profiler.BeginSample("Get Chunk");
            neighbours[i] = GetChunk(neighbourCoordinates);
            Profiler.EndSample();
        }
        return neighbours;
    }

    public bool TryGetBlocks(Vector3 position, NativeArray<int> blockIds)
    {
        throw new System.NotImplementedException();
    }

    public void SetChunkActive(int3 chunkCoordinates)
    {
        throw new System.NotImplementedException();
    }

    public bool TryPlaceBlock(Vector3 position, int blockID, ref int replacedBlockId)
    {
        throw new System.NotImplementedException();
    }

    public Structure GetStructure(int structureId)
    {
        return Structures[structureId];
    }

    public Vector3 GetPlayerPosition()
    {
        return Player.transform.position;
    }

    public RenderType GetRenderType()
    {
        return RenderType;
    }

    public void SetRenderDistance(int renderDistance)
    {   
        this.RenderDistance = renderDistance;
        _showWorld = true;
    }

    public void SetRenderType(RenderType type)
    {
        if (type == RenderType) return;

        _saveManager.UnloadChunks(this, activeChunksList);
        activeChunksList = new Dictionary<int3, IChunk>();
        RenderType = type;
        _showWorld = true;
    }
}

public enum RenderType
{
    GreedyMeshing,
    Instancing
}

