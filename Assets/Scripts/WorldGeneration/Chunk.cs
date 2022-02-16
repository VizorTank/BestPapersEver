using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class Chunk
{

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    ComputeShader shader;


    public World world;
    public Vector3Int coordinates;

    public static Vector3Int Size = new Vector3Int(8, 32, 8);

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<List<int>> triangles = new List<List<int>>();
    List<Vector2> uvs = new List<Vector2>();

    public int[,,] voxelMap = new int[Size.x, Size.y, Size.z];
    int[,,,] blockFaceCheck = new int[Size.x, Size.y, Size.z, 6];

    public bool IsActive
    {
        get { return chunkObject.activeSelf; }
        set { chunkObject.SetActive(value); }
    }

    public Vector3 ChunkPosition
    {
        get { return chunkObject.transform.position; }
    }

    public Chunk(Vector3Int _position, World _world)
    {
        coordinates = _position;
        world = _world;

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = Vector3.Scale(coordinates, Size);
        chunkObject.name = string.Format("Chunk {0}, {1}, {2}", coordinates.x, coordinates.y, coordinates.z);

        PopulateVoxelMap2();
    }

    public void DrawChunk()
    {
        PrepareTriangleData();
        CreateMeshData2();
        vertexIndex = 0;
        vertices = new List<Vector3>();
        triangles = new List<List<int>>();
        uvs = new List<Vector2>();
        PrepareTriangleData();
        CreateMeshData();
        CreateMesh();

        CreateMeshCollider();
    }

    void PrepareTriangleData()
    {
        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            triangles.Add(new List<int>());
        }
    }

    public void CreateMeshCollider()
    {
        meshCollider.sharedMesh = meshFilter.mesh;
    }

    static readonly ProfilerMarker profilerCreateMeshDataY = new ProfilerMarker("CreateMeshDataY");

    void CreateMeshData()
    {
        for (int y = 0; y < Size.y; y++)
        {
            //profilerCreateMeshDataY.Begin();
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                        AddVoxelDataToChunks(new Vector3(x, y, z));
                }
            }
            //profilerCreateMeshDataY.End();
        }
    }

    void CreateMeshData2()
    {
        ComputeBuffer blockTypeBuffer = new ComputeBuffer(world.blockTypes.Length, sizeof(int));
        ComputeBuffer blockListBuffer = new ComputeBuffer(Size.x * Size.y * Size.z, sizeof(int));
        ComputeBuffer blockFaceCheckBuffer = new ComputeBuffer(Size.x * Size.y * Size.z * 6, sizeof(int));

        int kernelIndex = world.shader.FindKernel("CSMain");

        int[] blockType = new int[world.blockTypes.Length];
        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            blockType[i] = world.blockTypes[i].isSolid ? 1 : 0;
            //blockType[i] = world.blockTypes[i].isSolid ? 1 : 0;
        }
        blockTypeBuffer.SetData(blockType);

        blockListBuffer.SetData(voxelMap);

        blockFaceCheckBuffer.SetData(blockFaceCheck);

        world.shader2.SetBuffer(kernelIndex, "BlockList", blockListBuffer);
        world.shader2.SetBuffer(kernelIndex, "BlockTypeIsSolid", blockTypeBuffer);
        world.shader2.SetBuffer(kernelIndex, "BlockFaceCheck", blockFaceCheckBuffer);

        world.shader2.SetInts("ChunkSize", new int[] { Size.x, Size.y, Size.z });

        world.shader2.Dispatch(kernelIndex, Size.x / 8, Size.y / 8, Size.z / 8);

        blockFaceCheckBuffer.GetData(blockFaceCheck);

        blockFaceCheckBuffer.Release();
        blockListBuffer.Release();
        blockTypeBuffer.Release();

        for (int y = 0; y < Size.y; y++)
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                        AddVoxelDataToChunks2(new Vector3(x, y, z));
                }
            }
        }
    }

    void AddVoxelDataToChunks2(Vector3 position)
    {
        for (int i = 0; i < 6; i++)
        {
            if (Convert.ToBoolean(blockFaceCheck[(int)position.x, (int)position.y, (int)position.z, i]))
            {
                int blockID = voxelMap[(int)position.x, (int)position.y, (int)position.z];
                for (int j = 0; j < 4; j++)
                {
                    vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[i, j]]);
                    uvs.Add(VoxelData.voxelUvs[j]);
                }
                triangles[blockID].Add(vertexIndex + 0);
                triangles[blockID].Add(vertexIndex + 1);
                triangles[blockID].Add(vertexIndex + 2);
                triangles[blockID].Add(vertexIndex + 2);
                triangles[blockID].Add(vertexIndex + 1);
                triangles[blockID].Add(vertexIndex + 3);
                vertexIndex += 4;
            }
        }
    }

    void AddVoxelDataToChunks(Vector3 position)
    {
        for (int i = 0; i < 6; i++)
        {
            if (!CheckVoxel(position + VoxelData.faceChecks[i]))
            {
                int blockID = voxelMap[(int)position.x, (int)position.y, (int)position.z];
                for (int j = 0; j < 4; j++)
                {
                    vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[i, j]]);
                    uvs.Add(VoxelData.voxelUvs[j]);
                }
                triangles[blockID].Add(vertexIndex + 0);
                triangles[blockID].Add(vertexIndex + 1);
                triangles[blockID].Add(vertexIndex + 2);
                triangles[blockID].Add(vertexIndex + 2);
                triangles[blockID].Add(vertexIndex + 1);
                triangles[blockID].Add(vertexIndex + 3);
                vertexIndex += 4;
            }
        }
    }

    void PopulateVoxelMap()
    {
        for (int y = 0; y < Size.y; y++)
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + ChunkPosition);
                    //voxelMap[x, y, z] = 2;// world.GetVoxel(new Vector3(x, y, z) + ChunkPosition);
                }
            }
        }
    }

    void PopulateVoxelMap2()
    {
        ComputeBuffer buffer = new ComputeBuffer(Size.x * Size.y * Size.z, sizeof(int));
        int kernelIndex = world.shader.FindKernel("CSMain");
        buffer.SetData(voxelMap);
        world.shader.SetBuffer(kernelIndex, "VoxelData", buffer);

        world.shader.SetInts("ChunkPosition", new int[] { (int)ChunkPosition.x, (int)ChunkPosition.y, (int)ChunkPosition.z});
        world.shader.SetInts("ChunkSize", new int[] { Size.x, Size.y, Size.z });
        world.shader.SetFloats("WorldSizeInVoxels", new float[]{ World.WorldSizeInVoxels.x, World.WorldSizeInVoxels.y, World.WorldSizeInVoxels.z});
        world.shader.SetFloat("NoiseOffset", world.NoiseOffset);
        world.shader.SetFloat("TerrainSize", world.biome.terrainSize / 50);
        world.shader.SetInt("TerrainHeight", world.biome.terrainHeight);
        world.shader.SetInt("SolidGroundHeight", world.biome.solidGroundHeight);

        world.shader.Dispatch(kernelIndex, Size.x / 8, Size.y / 8, Size.z / 8);

        buffer.GetData(voxelMap);
        buffer.Release();
    }

    bool isVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x >= Size.x ||
            y < 0 || y >= Size.y ||
            z < 0 || z >= Size.z)
            return false;
        return true;
    }

    bool CheckVoxel(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        if (!isVoxelInChunk(x, y, z))
            return world.CheckForVoxel(position + ChunkPosition);

        return world.blockTypes[voxelMap[x, y, z]].isSolid;
    }

    void CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            uv = uvs.ToArray(),
            subMeshCount = world.blockTypes.Length
        };

        for (int i = 0; i < triangles.Count; i++)
        {
            mesh.SetTriangles(triangles[i], i, true, 0);
        }

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        List<Material> materials = new List<Material>();
        foreach (var item in world.blockTypes)
        {
            materials.Add(item.material);
        }
        meshRenderer.materials = materials.ToArray();
    }
}
/*
public class BlockTypeForShader
{
    public int Id
}
//*/