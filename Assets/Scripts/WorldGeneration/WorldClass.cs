using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WorldClass : MonoBehaviour
{
    public int seed;

    public ComputeShader generateWorldShader;
    public ComputeShader createMeshShader;
    public GameObject blockEntityPrefab;

    public Transform player;
    public BiomeAttributes biome;
    public Vector3 spawnChunk;
    public BlockType[] blockTypes;
    public int ChunkType = 1;

    [SerializeField] public static readonly Vector3Int WorldSizeInChunks = new Vector3Int(16, 3, 16);
    public int ViewDistanceInChunks = 10;
    public static Vector3Int WorldSizeInVoxels
    {
        get { return Vector3Int.Scale(WorldSizeInChunks, ChunkCore.Size); }
    }

    ChunkCore[,,] chunks = new ChunkCore[WorldSizeInChunks.x, WorldSizeInChunks.y, WorldSizeInChunks.z];
    List<Vector3Int> activeChunks = new List<Vector3Int>();
    Vector3Int playerLastChunkCoord;

    public float NoiseOffset;

    private void Start()
    {

        UnityEngine.Random.InitState(seed);
        NoiseOffset = UnityEngine.Random.Range(1000.0f, 1000.0f);
        spawnChunk = new Vector3(WorldSizeInChunks.x / 2f, 0, WorldSizeInChunks.z / 2f);
        player.position = spawnChunk * ChunkCore.Size.x + new Vector3(0, ChunkCore.Size.y * WorldSizeInChunks.y, 0);
        GenerateWorld();
        //CreateNewChunk3(new Vector3Int(0, 0 ,0));
    }

    private void Update()
    {
        Vector3Int playerPos = GetPositionInChunks(player.position);
        if (playerLastChunkCoord != playerPos)
        {
            DrawWorld();
            playerLastChunkCoord = playerPos;
        }
    }

    void DrawWorld()
    {
        foreach (Vector3Int chunk in activeChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].DrawChunk();
            //chunks[chunk.x, chunk.y, chunk.z].CreateMeshFromEntities();
        }
    }

    void GenerateWorld()
    {
        Vector3Int renderCenterPos = GetPositionInChunks(player.position);

        //List<Vector3Int> previouslyActiveChunks = new List<Vector3Int>(activeChunks);

        for (int x = renderCenterPos.x - ViewDistanceInChunks; x < renderCenterPos.x + ViewDistanceInChunks; x++)
        {
            for (int y = 0; y < WorldSizeInChunks.y; y++)
            {
                for (int z = renderCenterPos.z - ViewDistanceInChunks; z < renderCenterPos.z + ViewDistanceInChunks; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    if (!isChunkInWorld(coord))
                        continue;
                    if (chunks[x, y, z] == null)
                    {
                        if (ChunkType == 1)
                            CreateNewChunk(coord);
                        if (ChunkType == 2)
                            CreateNewChunk2(coord);
                        if (ChunkType == 3)
                        {
                            CreateNewChunk3(coord);
                            //activeChunks.Add(coord);
                        }
                    }
                    else if (!chunks[x, y, z].IsActive)
                    {
                        chunks[x, y, z].IsActive = true;
                        activeChunks.Add(coord);
                    }
                    /*
                    for (int i = 0; i < previouslyActiveChunks.Count; i++)
                    {
                        if (previouslyActiveChunks[i] == coord)
                            previouslyActiveChunks.RemoveAt(i);
                    }
                    //*/
                }
            }
        }
        /*
        foreach (Vector3Int chunk in previouslyActiveChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].IsActive = false;
            activeChunks.Remove(chunk);
        }
        //*/
    }

    Vector3Int GetPositionInChunks(Vector3 position)
    {
        return new Vector3Int(Mathf.FloorToInt(position.x / ChunkCore.Size.x), Mathf.FloorToInt(position.y / ChunkCore.Size.y), Mathf.FloorToInt(position.z / ChunkCore.Size.z));
    }

    public byte GetVoxel(Vector3 position)
    {
        int yPos = Mathf.FloorToInt(position.y);
        if (!isVoxelInWorld(position))
            return 0;
        if (yPos == 0)
            return 1;

        int terrainHeight = Mathf.FloorToInt(
            Noise.Get2DPerlin(
                new Vector2(position.x, position.z), NoiseOffset, biome.terrainSize)
            * biome.terrainHeight) + biome.solidGroundHeight;
        byte voxelValue = 0;

        if (yPos > terrainHeight)
            return 0;

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos > terrainHeight - 4)
            voxelValue = 4;
        else if (yPos < terrainHeight)
            voxelValue = 2;

        if (voxelValue == 2)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                    if (Noise.Get3DPerlin(position, lode.noiseOffset + NoiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }

        return voxelValue;
    }

    public byte GetVoxel(float3 position)
    {
        int yPos = (int)math.floor(position.y);

        int terrainHeight = 8;
        byte voxelValue = 0;

        if (yPos > terrainHeight)
            return 0;

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos > terrainHeight - 4)
            voxelValue = 4;
        else if (yPos < terrainHeight)
            voxelValue = 2;

        return voxelValue;
    }

    public Entity GetVoxelEntity(float3 position)
    {
        float3 localPos = position % new float3(ChunkCore.Size.x, ChunkCore.Size.y, ChunkCore.Size.z);
        Vector3Int tmp = GetPositionInChunks(position);
        int3 chunkPos = new int3(tmp.x, tmp.y, tmp.z);

        if (CheckIfChunkIsNull(tmp))
            return Entity.Null;

        return chunks[chunkPos.x, chunkPos.y, chunkPos.z].GetVoxel(new int3(localPos));
    }

    public bool CheckForVoxel(Vector3 position)
    {
        Vector3Int localPos = new Vector3Int(
            Mathf.FloorToInt(position.x) % ChunkCore.Size.x,
            Mathf.FloorToInt(position.y) % ChunkCore.Size.y,
            Mathf.FloorToInt(position.z) % ChunkCore.Size.z);

        Vector3Int chunkPos = GetPositionInChunks(position);

        if (CheckIfChunkIsNull(chunkPos))
            return false;

        return blockTypes[chunks[chunkPos.x, chunkPos.y, chunkPos.z].voxelMap[localPos.x, localPos.y, localPos.z]].isSolid;
    }

    bool CheckIfChunkIsNull(Vector3Int chunkPos)
    {
        if (!isChunkInWorld(chunkPos))
            return true;
        if (chunks[chunkPos.x, chunkPos.y, chunkPos.z] == null)
            return true;
        return false;
    }

    void CreateNewChunk(Vector3Int coordinates)
    {
        // TODO: Center of World in (0, 0)
        chunks[coordinates.x, coordinates.y, coordinates.z] = new ChunkV1(coordinates, this);
        activeChunks.Add(coordinates);
    }

    void CreateNewChunk2(Vector3Int coordinates)
    {
        // TODO: Center of World in (0, 0)
        chunks[coordinates.x, coordinates.y, coordinates.z] = new ChunkV2(coordinates, this);
        activeChunks.Add(coordinates);
    }

    void CreateNewChunk3(Vector3Int coordinates)
    {
        // TODO: Center of World in (0, 0)
        chunks[coordinates.x, coordinates.y, coordinates.z] = new ChunkEntities(coordinates, this);
        activeChunks.Add(coordinates);
    }

    bool isChunkInWorld(Vector3Int coord)
    {
        if (coord.x < 0 || coord.x >= WorldSizeInChunks.x ||
            coord.y < 0 || coord.y >= WorldSizeInChunks.y ||
            coord.z < 0 || coord.z >= WorldSizeInChunks.z)
            return false;
        return true;
    }

    bool isVoxelInWorld(Vector3 position)
    {
        if (position.x < 0 || position.x >= WorldSizeInVoxels.x ||
            position.y < 0 || position.y >= WorldSizeInVoxels.y ||
            position.z < 0 || position.z >= WorldSizeInVoxels.z)
            return false;
        return true;
    }
}

[System.Serializable]
public class BlockType
{
    public string name;
    public Material material;
    public bool isSolid;
}