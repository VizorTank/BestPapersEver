using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class WorldClass : MonoBehaviour
{
    public static int WorldCubeSize = 16;
    public static readonly int3 WorldSizeInChunks = new int3(WorldCubeSize, 16, WorldCubeSize);
    private static int3 ChunkSize { get => VoxelData.ChunkSize; }

    public BlockTypesList blockTypesList;
    public List<Material> Materials
    {
        get => blockTypesList.Materials;
    }

    public BiomeAttributes BiomeAttributes;
    public BiomeAttributesStruct BiomeAttributesStruct;

    private Chunk[,,] chunks = new Chunk[WorldSizeInChunks.x, WorldSizeInChunks.y, WorldSizeInChunks.z];
    private List<int3> activeChunks = new List<int3>();

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        BiomeAttributesStruct = BiomeAttributes.GetBiomeStruct();
        blockTypesList.ProcessData();


        GenerateWorld();
        LinkChunks();
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
        chunks[position.x, position.y, position.z].neighbours = chunkNeighbours;
    }

    // Update is called once per frame
    void Update()
    {
        DrawWorldWithEntities();
    }

    private void DrawWorldWithEntities()
    {
        foreach (Chunk chunk in chunks)
        {
            chunk.CreateBlockIdCopy();
        }
        foreach (Chunk chunk in chunks)
        {
            chunk.CreateClasters();
        }
        foreach (Chunk chunk in chunks)
        {
            chunk.CheckClusterVisibility();
        }
        foreach (Chunk chunk in chunks)
        {
            chunk.CreateMeshWithClasters();
        }
        //foreach (Chunk chunk in chunks)
        //{
        //    chunk.CreateMesh();
        //}

        foreach (Chunk chunk in chunks)
        {
            chunk.LoadMesh();
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
                    CreateNewChunk3(coord);
                }
            }
        }
    }

    void CreateNewChunk3(int3 coordinates)
    {
        // TODO: Center of World in (0, 0)
        chunks[coordinates.x, coordinates.y, coordinates.z] = new Chunk(coordinates, this, BiomeAttributesStruct);
        activeChunks.Add(coordinates);
    }

    public int SetBlock(Vector3 position, int blockID)
    {
        Debug.Log("Block Placing");
        int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);

        int3 cPos = new int3(Mathf.FloorToInt((float)pos.x / ChunkSize.x),
            Mathf.FloorToInt((float)pos.y / ChunkSize.y),
            Mathf.FloorToInt((float)pos.z / ChunkSize.z));
        if (cPos.x >= 0 && cPos.x < WorldSizeInChunks.x &&
            cPos.y >= 0 && cPos.y < WorldSizeInChunks.y &&
            cPos.z >= 0 && cPos.z < WorldSizeInChunks.z)
        {
            return chunks[cPos.x, cPos.y, cPos.z].SetBlock(
                pos % ChunkSize,
                blockID);
        }
        return 0;
    }

    public int GetBlock(Vector3 position)
    {
        int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);

        int3 cPos = new int3(Mathf.FloorToInt((float)pos.x / ChunkSize.x),
            Mathf.FloorToInt((float)pos.y / ChunkSize.y),
            Mathf.FloorToInt((float)pos.z / ChunkSize.z));
        if (cPos.x >= 0 && cPos.x < WorldSizeInChunks.x &&
            cPos.y >= 0 && cPos.y < WorldSizeInChunks.y &&
            cPos.z >= 0 && cPos.z < WorldSizeInChunks.z)
        {
            return chunks[cPos.x, cPos.y, cPos.z].GetBlock(
                pos % ChunkSize);
        }
        return 0;
    }
}

