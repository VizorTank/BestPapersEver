using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public int seed;

    public Transform player;
    public BiomeAttributes biome;
    public Vector3 spawnChunk;
    public BlockType[] blockTypes;

    public static readonly Vector3Int WorldSizeInChunks = new Vector3Int(100, 8, 100);
    public int ViewDistanceInChunks = 10;
    public static Vector3Int WorldSizeInVoxels
    {
        get { return Vector3Int.Scale(WorldSizeInChunks, Chunk.Size); }
    }

    Chunk[,,] chunks = new Chunk[WorldSizeInChunks.x, WorldSizeInChunks.y, WorldSizeInChunks.z];
    List<Vector3Int> activeChunks = new List<Vector3Int>();
    Vector3Int playerLastChunkCoord;

    private void Start()
    {
        Random.InitState(seed);
        spawnChunk = new Vector3(WorldSizeInChunks.x / 2f, 0, WorldSizeInChunks.z / 2f);
        player.position = spawnChunk * Chunk.Size.x + new Vector3(0, Chunk.Size.y, 0);
        //GenerateWorld();
    }

    private void Update()
    {
        Vector3Int playerPos = GetPositionInChunks(player.position);
        if (playerLastChunkCoord != playerPos)
        {
            GenerateWorld();
            playerLastChunkCoord = playerPos;
        }
    }

    void GenerateWorld()
    {
        Vector3Int renderCenterPos = GetPositionInChunks(player.position);

        List<Vector3Int> previouslyActiveChunks = new List<Vector3Int>(activeChunks);

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
                        CreateNewChunk(coord);
                    else if (!chunks[x, y, z].IsActive)
                    {
                        chunks[x, y, z].IsActive = true;
                        activeChunks.Add(coord);
                    }
                    for (int i = 0; i < previouslyActiveChunks.Count; i++)
                    {
                        if (previouslyActiveChunks[i] == coord)
                            previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (Vector3Int chunk in previouslyActiveChunks)
        {
            chunks[chunk.x, chunk.y, chunk.z].IsActive = false;
            activeChunks.Remove(chunk);
        }
    }

    Vector3Int GetPositionInChunks(Vector3 position)
    {
        return new Vector3Int(Mathf.FloorToInt(position.x / Chunk.Size.x), 0, Mathf.FloorToInt(position.z / Chunk.Size.z));
    }

    public byte GetVoxel(Vector3 position)
    {
        int yPos = Mathf.FloorToInt(position.y);
        if (!isVoxelInWorld(position))
            return 0;
        if (yPos == 0)
            return 1;

        int terrainHeight = Mathf.FloorToInt(Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.terrainSize) * biome.terrainHeight) + biome.solidGroundHeight;
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
                    if (Noise.Get3DPerlin(position, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }

        return voxelValue;
    }

    void CreateNewChunk(Vector3Int coordinates)
    {
        // TODO: Center of World in (0, 0)
        chunks[coordinates.x, coordinates.y, coordinates.z] = new Chunk(coordinates, this);
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