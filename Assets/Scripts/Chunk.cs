using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    public World world;
    public Vector3Int coordinates;

    public static Vector3Int Size = new Vector3Int(32, 16, 32);

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<List<int>> triangles = new List<List<int>>();
    List<Vector2> uvs = new List<Vector2>();

    byte[,,] voxelMap = new byte[Size.x, Size.y, Size.z];

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
        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = Vector3.Scale(coordinates, Size);
        chunkObject.name = string.Format("Chunk {0}, {1}, {2}", coordinates.x, coordinates.y, coordinates.z);

        PrepareTriangleData();
        PopulateVoxelMap();
        CreateMeshData();
        CreateMesh();
    }
    void CreateMeshData()
    {
        for (int y = 0; y < Size.y; y++)
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                        AddVoxelDataToChunks(new Vector3(x, y, z));
                }
            }
        }
    }

    void PrepareTriangleData()
    {
        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            triangles.Add(new List<int>());
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
                }
            }
        }
    }
    
    void AddVoxelDataToChunks(Vector3 position)
    {
        for (int i = 0; i < 6; i++)
        {
            if (!CheckVoxel(position + VoxelData.faceChecks[i]))
            {
                byte blockID = voxelMap[(int)position.x, (int)position.y, (int)position.z];
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

    bool isVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x >= Size.x ||
            y < 0 || y > Size.y - 1 ||
            z < 0 || z > Size.z - 1)
            return false;
        return true;
    }

    bool CheckVoxel(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        if (!isVoxelInChunk(x, y, z))
            return world.blockTypes[world.GetVoxel(position + ChunkPosition)].isSolid;

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
