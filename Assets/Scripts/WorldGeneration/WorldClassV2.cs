using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldClassV2 : MonoBehaviour
{
    public static int WorldCubeSize = 8;
    public static readonly Vector3Int WorldSizeInChunks = new Vector3Int(WorldCubeSize, 4, WorldCubeSize);
    public BlockType[] blockTypes;
    public List<Material> materials;

    private ChunkV4[,,] chunks = new ChunkV4[WorldSizeInChunks.x, WorldSizeInChunks.y, WorldSizeInChunks.z];
    private List<Vector3Int> activeChunks = new List<Vector3Int>();

    // Start is called before the first frame update
    void Start()
    {
        materials = new List<Material>();
        foreach (var item in blockTypes)
        {
            materials.Add(item.material);
        }
        GenerateWorld();
    }

    // Update is called once per frame
    void Update()
    {
        DrawWorldWithEntities();
    }

    private void DrawWorldWithEntities()
    {
        foreach (Vector3Int chunk in activeChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].GenerateMeshWithJobs();
            //chunks[chunk.x, chunk.y, chunk.z].CreateMeshFromEntities();
        }

        foreach (Vector3Int chunk in activeChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].GenerateMeshWithJobsGetData2();
            //chunks[chunk.x, chunk.y, chunk.z].CreateMeshFromEntities();
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
        chunks[coordinates.x, coordinates.y, coordinates.z] = new ChunkV4(coordinates, this);
        activeChunks.Add(coordinates);
    }
}
