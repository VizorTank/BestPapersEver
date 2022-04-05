using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class WorldClassV3 : MonoBehaviour
{
    public static int WorldCubeSize = 8;
    public static readonly Vector3Int WorldSizeInChunks = new Vector3Int(WorldCubeSize, 2, WorldCubeSize);
    public BlockType[] blockTypes;
    public List<Material> materials
    {
        get => blockTypesDoP.materials;
    }

    private ChunkV5[,,] chunks = new ChunkV5[WorldSizeInChunks.x, WorldSizeInChunks.y, WorldSizeInChunks.z];
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

    float Total = 0;
    int amount = 0;
    bool started = false;
    private void DrawWorldWithEntities()
    {

        foreach (ChunkV5 chunk in chunks)
        {
            amount += chunk.GenerateMeshWithJobs();
        }
        foreach (Vector3Int chunk in activeChunks)
        {
            float a = chunks[chunk.x, chunk.y, chunk.z].GenerateMeshWithJobsGetData2();
            Total += a;
            if (a > 0)
                amount -= 1;
        }

        if (amount == 0 && started)
        {
            Debug.Log("Avg: " + (Total / (WorldSizeInChunks.x * WorldSizeInChunks.y * WorldSizeInChunks.z)));
            amount--;
        }
        if (amount > 0)
            started = true;
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
        chunks[coordinates.x, coordinates.y, coordinates.z] = new ChunkV5(coordinates, this);
        activeChunks.Add(coordinates);
    }

    public int SetBlock(Vector3 position, int blockID)
    {
        Debug.Log("Block Placing");
        Vector3Int pos = new Vector3Int((int)position.x, (int)position.y, (int)position.z);

        Vector3Int cPos = new Vector3Int(Mathf.FloorToInt((float)pos.x / ChunkV5.Size.x),
            Mathf.FloorToInt((float)pos.y / ChunkV5.Size.y),
            Mathf.FloorToInt((float)pos.z / ChunkV5.Size.z));
        if (cPos.x < WorldSizeInChunks.x &&
            cPos.y < WorldSizeInChunks.y &&
            cPos.z < WorldSizeInChunks.z)
        {
            return chunks[cPos.x, cPos.y, cPos.z].SetBlock(
                new Unity.Mathematics.int3(
                    pos.x % ChunkV5.Size.x,
                    pos.y % ChunkV5.Size.y,
                    pos.z % ChunkV5.Size.z),
                blockID);
        }
        return int.MaxValue;
    }
}

