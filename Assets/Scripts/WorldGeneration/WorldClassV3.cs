using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class WorldClassV3 : MonoBehaviour
{
    public static int WorldCubeSize = 16;
    public static readonly Vector3Int WorldSizeInChunks = new Vector3Int(WorldCubeSize, 16, WorldCubeSize);
    public BlockType[] blockTypes;
    public List<Material> materials
    {
        get => blockTypesDoP.materials;
    }

    private ChunkV6[,,] chunks = new ChunkV6[WorldSizeInChunks.x, WorldSizeInChunks.y, WorldSizeInChunks.z];
    private List<Vector3Int> activeChunks = new List<Vector3Int>();

    public BlockTypesDoP blockTypesDoP;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        blockTypesDoP = new BlockTypesDoP(blockTypes);

        GenerateWorld();
    }

    // Update is called once per frame
    void Update()
    {
        DrawWorldWithEntities();
    }

    private void DrawWorldWithEntities()
    {
        foreach (ChunkV6 chunk in chunks)
        {
            chunk.GenerateClastersWithJobs();
        }
        foreach (ChunkV6 chunk in chunks)
        {
            chunk.CheckClusterVisibilityWithJobs();;
        }
        foreach (ChunkV6 chunk in chunks)
        {
            chunk.GenerateMeshWithJobs();
        }
        foreach (ChunkV6 chunk in chunks)
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
        blockTypesDoP.Destroy();
    }
    private void GenerateWorld()
    {
        for (int x = 0; x < WorldSizeInChunks.x; x++)
        {
            for (int y = 0; y < WorldSizeInChunks.y; y++)
            {
                for (int z = 0; z < WorldSizeInChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    CreateNewChunk3(coord);
                }
            }
        }
    }

    void CreateNewChunk3(Vector3Int coordinates)
    {
        // TODO: Center of World in (0, 0)
        chunks[coordinates.x, coordinates.y, coordinates.z] = new ChunkV6(coordinates, this);
        activeChunks.Add(coordinates);
    }

    public int SetBlock(Vector3 position, int blockID)
    {
        Debug.Log("Block Placing");
        Vector3Int pos = new Vector3Int((int)position.x, (int)position.y, (int)position.z);

        Vector3Int cPos = new Vector3Int(Mathf.FloorToInt((float)pos.x / ChunkV6.Size.x),
            Mathf.FloorToInt((float)pos.y / ChunkV6.Size.y),
            Mathf.FloorToInt((float)pos.z / ChunkV6.Size.z));
        if (cPos.x >= 0 && cPos.x < WorldSizeInChunks.x &&
            cPos.y >= 0 && cPos.y < WorldSizeInChunks.y &&
            cPos.z >= 0 && cPos.z < WorldSizeInChunks.z)
        {
            return chunks[cPos.x, cPos.y, cPos.z].SetBlock(
                new Unity.Mathematics.int3(
                    pos.x % ChunkV6.Size.x,
                    pos.y % ChunkV6.Size.y,
                    pos.z % ChunkV6.Size.z),
                blockID);
        }
        return 0;
    }

    public int GetBlock(Vector3 position)
    {
        Vector3Int pos = new Vector3Int((int)position.x, (int)position.y, (int)position.z);

        int3 chunkSize = ChunkV6.Size;

        Vector3Int cPos = new Vector3Int(Mathf.FloorToInt((float)pos.x / chunkSize.x),
            Mathf.FloorToInt((float)pos.y / chunkSize.y),
            Mathf.FloorToInt((float)pos.z / chunkSize.z));
        if (cPos.x >= 0 && cPos.x < WorldSizeInChunks.x &&
            cPos.y >= 0 && cPos.y < WorldSizeInChunks.y &&
            cPos.z >= 0 && cPos.z < WorldSizeInChunks.z)
        {
            return chunks[cPos.x, cPos.y, cPos.z].GetBlock(
                new int3(
                    pos.x % chunkSize.x,
                    pos.y % chunkSize.y,
                    pos.z % chunkSize.z));
        }
        return 0;
    }
}

