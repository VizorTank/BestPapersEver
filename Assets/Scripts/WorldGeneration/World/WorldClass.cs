using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class WorldClass : MonoBehaviour
{
    public GameObject Player;

    public int renderDistance = 16;
    public static int WorldCubeSize = 64;
    public int3 WorldSizeInChunks = new int3(WorldCubeSize, 2, WorldCubeSize);
    private static int3 ChunkSize { get => VoxelData.ChunkSize; }

    public BlockTypesList blockTypesList;
    public List<Material> Materials
    {
        get => blockTypesList.Materials;
    }
    public List<Structure> Structures = new List<Structure>();

    public BiomeAttributes BiomeAttributes;
    public BiomeAttributesStruct BiomeAttributesStruct;

    private IChunk[,,] chunks;
    private bool[,,] activeChunks;
    private List<int3> activeChunksList = new List<int3>();
    private int prevRenderDistanceSize;
    private int renderDistanceSize;
    //private List<int3> activeChunks = new List<int3>();

    [Range(0, 1)]
    public float GlobalLightLevel = 1;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        BiomeAttributesStruct = BiomeAttributes.GetBiomeStruct();
        blockTypesList.ProcessData();

        CreateTreeStructure();

        chunks = new IChunk[WorldSizeInChunks.x, WorldSizeInChunks.y, WorldSizeInChunks.z];

        if (Player != null)
        {
            Player.transform.position = new Vector3(VoxelData.ChunkSize.x * WorldSizeInChunks.x / 2, 
                VoxelData.ChunkSize.y * WorldSizeInChunks.y, 
                VoxelData.ChunkSize.z * WorldSizeInChunks.z / 2);
        }

        //GenerateWorld();
        //LinkChunks();
        CreateActiveChunkArray();
    }

    public BiomeAttributesStruct GetBiome(int3 ChunkCoord)
    {
        return BiomeAttributesStruct;
    }
    
    public void CreateActiveChunkArray()
    {
        prevRenderDistanceSize = renderDistance * 2 + 1;
        activeChunks = new bool[prevRenderDistanceSize, WorldSizeInChunks.y, prevRenderDistanceSize];
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

    // private void LinkChunks()
    // {
    //     for (int x = 0; x < WorldSizeInChunks.x; x++)
    //     {
    //         for (int y = 0; y < WorldSizeInChunks.y; y++)
    //         {
    //             for (int z = 0; z < WorldSizeInChunks.z; z++)
    //             {
    //                 LinkChunk(new int3(x, y, z));
    //             }
    //         }
    //     }
    // }

    // private void LinkChunk(int3 position)
    // {
    //     ChunkNeighbours chunkNeighbours = new ChunkNeighbours();
    //     for (int i = 0; i < 6; i++)
    //     {
    //         int3 neighbourPos = position + VoxelData.voxelNeighbours[i];
    //         if (neighbourPos.x < 0 || neighbourPos.x >= WorldSizeInChunks.x ||
    //             neighbourPos.y < 0 || neighbourPos.y >= WorldSizeInChunks.y ||
    //             neighbourPos.z < 0 || neighbourPos.z >= WorldSizeInChunks.z) continue;
    //         chunkNeighbours[i] = chunks[neighbourPos.x, neighbourPos.y, neighbourPos.z];
    //     }
    //     chunks[position.x, position.y, position.z].SetNeighbours(chunkNeighbours);
    // }

    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalFloat("GlobalLightLevel", 1 - GlobalLightLevel);
        ShowWorld();
        DrawChunks();
    }

    public int3 playerLastChunkPos = new int3();

    // Trash
    // Please remove future me
    private void ShowWorld()
    {
        //if (!IsInWorld(Player.transform.position)) return;
        int3 playerChunkPos = GetChunkCoords(Player.transform.position);
        playerChunkPos.y = 0;
        int3 pPosDiff = playerChunkPos - playerLastChunkPos;
        if (math.all(pPosDiff == 0))
        {
            activeChunksList.Clear();
            renderDistanceSize = renderDistance * 2 + 1;
            bool[,,] removeChunks = activeChunks;
            activeChunks = new bool[renderDistanceSize, WorldSizeInChunks.y, renderDistanceSize];
            for (int x = 0; x < renderDistanceSize; x++)
            {
                for (int z = 0; z < renderDistanceSize; z++)
                {
                    for (int y = 0; y < WorldSizeInChunks.y; y++)
                    {
                        CreateNewChunk(new int3(x - renderDistance - 1, y, z - renderDistance - 1) + playerChunkPos);
                        activeChunks[x, y, z] = true;
                        activeChunksList.Add(new int3(x - renderDistance - 1, y, z - renderDistance - 1) + playerChunkPos);
                    }
                }
            }

            for (int x = 0; x < prevRenderDistanceSize; x++)
            {
                for (int z = 0; z < prevRenderDistanceSize; z++)
                {
                    for (int y = 0; y < WorldSizeInChunks.y; y++)
                    {
                        if (removeChunks[x, y, z] && !activeChunks[x + pPosDiff.x, y, z + pPosDiff.z])
                            chunks[x + playerLastChunkPos.x, y, z + playerLastChunkPos.z].Hide();
                    }
                }
            }
            prevRenderDistanceSize = renderDistanceSize;
            //foreach (int3 int3 in activeChunks)
            //{
            //    removeChunks.Remove(int3);
            //    LinkChunk(int3);
            //}
            //foreach (int3 item in removeChunks)
            //{
            //    chunks[item.x, item.y, item.z].Destroy();
            //    chunks[item.x, item.y, item.z] = null;
            //}
        }
        playerLastChunkPos = playerChunkPos;
    }

    private void DrawChunks()
    {
        // 1 for TODO
        for (int x = 0; x < renderDistanceSize; x++)
        {
            for (int z = 0; z < renderDistanceSize; z++)
            {
                for (int y = 0; y < WorldSizeInChunks.y; y++)
                {
                    int3 chunk = new int3(x - renderDistance - 1, y, z - renderDistance - 1) + playerLastChunkPos;
                    if (chunks[chunk.x, chunk.y, chunk.z] != null)
                    {
                        chunks[chunk.x, chunk.y, chunk.z].Render();
                    }
                }
            }
        }
    }

    void OnDestroy()
    {
        for (int x = 0; x < WorldSizeInChunks.x; x++)
        {
            for (int y = 0; y < WorldSizeInChunks.y; y++)
            {
                for (int z = 0; z < WorldSizeInChunks.z; z++)
                {
                    if (chunks[x, y, z] != null)
                        chunks[x, y, z].Destroy();
                }
            }
        }
        blockTypesList.Destroy();
        BiomeAttributesStruct.lodes.Dispose();
    }
    private void GenerateWorld()
    {
        for (int x = 0; x < WorldSizeInChunks.x; x++)
        {
            for (int y = 0; y < WorldSizeInChunks.y; y++)
            {
                for (int z = 0; z < WorldSizeInChunks.z; z++)
                {
                    int3 coord = new int3(x, y, z);
                    CreateNewChunk(coord);
                }
            }
        }
    }

    private void CreateNewChunk(int x, int y, int z) => CreateNewChunk(new int3(x, y, z));

    public bool IsCoordInWorld(int3 coordinates)
    {
        if (coordinates.x < 0 || coordinates.x >= WorldSizeInChunks.x ||
            coordinates.y < 0 || coordinates.y >= WorldSizeInChunks.y ||
            coordinates.z < 0 || coordinates.z >= WorldSizeInChunks.z) return false;
        return true;
    }

    private void CreateNewChunk(int3 coordinates)
    {
        // TODO: Center of World in (0, 0)
        if (!IsCoordInWorld(coordinates)) return;

        if (chunks[coordinates.x, coordinates.y, coordinates.z] == null)
            chunks[coordinates.x, coordinates.y, coordinates.z] = new Chunk(this, coordinates);
    }

    public bool TryPlaceBlock(Vector3 position, int blockID)
    {
        if (!IsInWorld(position)) return false;
        int3 chunkPos = GetChunkCoords(position);
        if (chunks[chunkPos.x, chunkPos.y, chunkPos.z] == null) return false;
        if (!chunks[chunkPos.x, chunkPos.y, chunkPos.z].TryGetBlock(new int3(position) % ChunkSize, out int replacedBlockId)) return false;
        if (!blockTypesList.areReplacable[replacedBlockId]) return false;
        return chunks[chunkPos.x, chunkPos.y, chunkPos.z].TrySetBlock(new int3(position) % ChunkSize, blockID, out int result);
    }

    public bool TrySetBlock(Vector3 position, int blockID, ref int replacedBlockId)
    {
        if (!IsInWorld(position)) return false;
        int3 chunkPos = GetChunkCoords(position);
        if (chunks[chunkPos.x, chunkPos.y, chunkPos.z] == null) return false;
        bool result = chunks[chunkPos.x, chunkPos.y, chunkPos.z].TrySetBlock(new int3(position) % ChunkSize, blockID, out int retBlockId);
        replacedBlockId = retBlockId;
        return result;
    }

    public int SetBlock(Vector3 position, int blockID)
    {
        int3 chunkPos = GetChunkCoords(position);
        chunks[chunkPos.x, chunkPos.y, chunkPos.z].TrySetBlock(new int3(position) % ChunkSize, blockID, out int result);
        return result;
    }

    public void CreateStructure(Vector3 position, int strucutreId)
    {
        // int3 chunkPos = GetChunkCoords(position);
        // chunks[chunkPos.x, chunkPos.y, chunkPos.z].CreateStructure(new int3(position) % ChunkSize, strucutreId);
    }

    public bool TryGetBlock(Vector3 position, ref int replacedBlockId)
    {
        if (!IsInWorld(position)) return false;
        int3 chunkPos = GetChunkCoords(position);
        if (chunks[chunkPos.x, chunkPos.y, chunkPos.z] == null) return false;
        bool tmp = chunks[chunkPos.x, chunkPos.y, chunkPos.z].TryGetBlock(new int3(position) % ChunkSize, out int result);
        replacedBlockId = result;
        return tmp;
    }

    public int GetBlock(Vector3 position)
    {
        int3 chunkPos = GetChunkCoords(position);
        chunks[chunkPos.x, chunkPos.y, chunkPos.z].TryGetBlock(new int3(position) % ChunkSize, out int result);
        return result;
    }
    public bool IsInWorld(float3 position) => IsInWorld(GetChunkCoords(position));
    public bool IsInWorld(int3 position)
    {
        if (position.x >= 0 && position.x < WorldSizeInChunks.x &&
            position.y >= 0 && position.y < WorldSizeInChunks.y &&
            position.z >= 0 && position.z < WorldSizeInChunks.z) return true;
        return false;
    }

    public int3 GetChunkCoords(float3 position)
    {
        int3 cPos = new int3(Mathf.FloorToInt(position.x / ChunkSize.x),
            Mathf.FloorToInt(position.y / ChunkSize.y),
            Mathf.FloorToInt(position.z / ChunkSize.z));
        return cPos;
    }

    public ChunkNeighbours GetNeighbours(int3 chunkCoordinates)
    {
        ChunkNeighbours neighbours = new ChunkNeighbours();

        for (int i = 0; i < 6; i++)
        {
            int3 tmp = chunkCoordinates + VoxelData.voxelNeighbours[i];
            if (IsInWorld(tmp) && chunks[tmp.x, tmp.y, tmp.z] != null)
                neighbours[i] = chunks[tmp.x, tmp.y, tmp.z];
            else
                neighbours[i] = null;
        }

        return neighbours;
    }
}

