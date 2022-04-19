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

    public BiomeAttributes BiomeAttributes;
    public BiomeAttributesStruct BiomeAttributesStruct;

    private Chunk[,,] chunks;
    private List<int3> activeChunks = new List<int3>();

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        BiomeAttributesStruct = BiomeAttributes.GetBiomeStruct();
        blockTypesList.ProcessData();

        chunks = new Chunk[WorldSizeInChunks.x, WorldSizeInChunks.y, WorldSizeInChunks.z];

        if (Player != null)
        {
            Player.transform.position = new Vector3(VoxelData.ChunkSize.x * WorldSizeInChunks.x / 2, 
                VoxelData.ChunkSize.y * WorldSizeInChunks.y, 
                VoxelData.ChunkSize.z * WorldSizeInChunks.z / 2);
        }

        //GenerateWorld();
        //LinkChunks();
    }

    private void LinkChunks()
    {
        for (int x = 0; x < WorldSizeInChunks.x; x++)
        {
            for (int y = 0; y < WorldSizeInChunks.y; y++)
            {
                for (int z = 0; z < WorldSizeInChunks.z; z++)
                {
                    LinkChunk(new int3(x, y, z));
                }
            }
        }
    }

    private void LinkChunk(int3 position)
    {
        ChunkNeighbours chunkNeighbours = new ChunkNeighbours();
        for (int i = 0; i < 6; i++)
        {
            int3 neighbourPos = position + VoxelData.voxelNeighbours[i];
            if (neighbourPos.x < 0 || neighbourPos.x >= WorldSizeInChunks.x ||
                neighbourPos.y < 0 || neighbourPos.y >= WorldSizeInChunks.y ||
                neighbourPos.z < 0 || neighbourPos.z >= WorldSizeInChunks.z) continue;
            chunkNeighbours[i] = chunks[neighbourPos.x, neighbourPos.y, neighbourPos.z];
        }
        chunks[position.x, position.y, position.z].SetNeighbours(chunkNeighbours);
    }

    // Update is called once per frame
    void Update()
    {
        ShowWorld();
        DrawChunks();
    }

    public int3 playerLastChunkPos = new int3();

    private void ShowWorld()
    {
        int3 playerPos = GetChunkCoords(Player.transform.position);
        if (!playerLastChunkPos.Equals(playerPos))
        {
            playerLastChunkPos = playerPos;
            List<int3> removeChunks = new List<int3>(activeChunks);
            activeChunks.Clear();
            for (int x = -renderDistance; x < renderDistance; x++)
            {
                for (int z = -renderDistance; z < renderDistance; z++)
                {
                    for (int y = -renderDistance; y < WorldSizeInChunks.y; y++)
                    {
                        CreateNewChunk(new int3(x, y, z) + playerPos);
                    }
                }
            }
            foreach (int3 int3 in activeChunks)
            {
                removeChunks.Remove(int3);
            }
            foreach (int3 item in removeChunks)
            {
                chunks[item.x, item.y, item.z].Destroy();
                chunks[item.x, item.y, item.z] = null;
            }
        }
    }

    private void DrawChunks()
    {
        foreach (int3 chunk in activeChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].ChunkGenerator.CreateBlockIdCopy();
        }
        foreach (int3 chunk in activeChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].ChunkGenerator.CreateClasters();
        }
        foreach (int3 chunk in activeChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].ChunkGenerator.CheckClusterVisibility();
        }
        foreach (int3 chunk in activeChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].ChunkGenerator.CreateMeshWithClasters();
        }
        foreach (int3 chunk in activeChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].ChunkGenerator.LoadMesh();
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

    void CreateNewChunk(int3 coordinates)
    {
        // TODO: Center of World in (0, 0)
        if (coordinates.x < 0 || coordinates.x >= WorldSizeInChunks.x ||
            coordinates.y < 0 || coordinates.y >= WorldSizeInChunks.y ||
            coordinates.z < 0 || coordinates.z >= WorldSizeInChunks.z) return;

        if (chunks[coordinates.x, coordinates.y, coordinates.z] == null)
            chunks[coordinates.x, coordinates.y, coordinates.z] = new Chunk(coordinates, this, BiomeAttributesStruct);
        activeChunks.Add(coordinates);
    }

    public bool TryPlaceBlock(Vector3 position, int blockID)
    {
        try
        {
            int3 chunkPos = GetChunkCoords(position);
            return chunks[chunkPos.x, chunkPos.y, chunkPos.z].TryPlaceBlock(new int3(position) % ChunkSize, blockID);
        }
        catch (Exception e)
        {
            MyLogger.DisplayWarning(e.Message);
            return false;
        }
    }

    public int SetBlock(Vector3 position, int blockID)
    {
        try
        {
            int3 chunkPos = GetChunkCoords(position);
            return chunks[chunkPos.x, chunkPos.y, chunkPos.z].SetBlock(new int3(position) % ChunkSize, blockID);
        }
        catch (Exception e)
        {
            MyLogger.DisplayWarning(e.Message);
            return 0;
        }
    }

    public int GetBlock(Vector3 position)
    {
        try
        {
            int3 chunkCoords = GetChunkCoords(position);
            return chunks[chunkCoords.x, chunkCoords.y, chunkCoords.z].GetBlock(new int3(position) % ChunkSize);
        }
        catch (Exception e)
        {
            MyLogger.DisplayWarning(e.Message);
            return 0;
        }
    }
    public int3 GetChunkCoords(float3 position)
    {
        int3 cPos = new int3(Mathf.FloorToInt(position.x / ChunkSize.x),
            Mathf.FloorToInt(position.y / ChunkSize.y),
            Mathf.FloorToInt(position.z / ChunkSize.z));
        if (cPos.x >= 0 && cPos.x < WorldSizeInChunks.x &&
            cPos.y >= 0 && cPos.y < WorldSizeInChunks.y &&
            cPos.z >= 0 && cPos.z < WorldSizeInChunks.z) return cPos;
        throw new Exception("Position outside of world.");
    }
}

