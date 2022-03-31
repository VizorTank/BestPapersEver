using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ChunkCore// : MonoBehaviour
{

    public Vector3Int coordinates;

    protected GameObject chunkObject;
    protected MeshRenderer meshRenderer;
    protected MeshFilter meshFilter;
    protected MeshCollider meshCollider;

    public int[,,] voxelMap = new int[Size.x, Size.y, Size.z];

    protected int vertexIndex;
    protected List<Vector3> vertices;
    protected List<List<int>> triangles;
    protected List<Vector2> uvs;

    public static int cubeSize = 32;
    public static Vector3Int Size = new Vector3Int(cubeSize, cubeSize, cubeSize);
    public WorldClass world;

    public bool IsActive
    {
        get { return chunkObject.activeSelf; }
        set { chunkObject.SetActive(value); }
    }

    public virtual void DrawChunk()
    {
        DrawChunkBefore();
        AddvoxelsToMesh();
        DrawChunkAfter();
    }

    public virtual void CreateMeshFromEntities() { }

    protected virtual void DrawChunkBefore()
    {
        PrepareTriangleData();
    }

    protected virtual void DrawChunkAfter()
    {
        CreateMesh();
        //CreateMeshCollider();
    }

    public ChunkCore(Vector3Int _position, WorldClass _world)
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
    }

    private void Awake()
    {
        
    }

    public Vector3 ChunkPosition
    {
        get { return chunkObject.transform.position; }
    }

    protected void PrepareTriangleData()
    {
        vertexIndex = 0;
        vertices = new List<Vector3>();
        triangles = new List<List<int>>();
        uvs = new List<Vector2>();
        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            triangles.Add(new List<int>());
        }
    }

    protected void CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            uv = uvs.ToArray(),
            subMeshCount = world.blockTypes.Length
        };

        for (int i = 0; i < world.blockTypes.Length; i++)
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

    protected void CreateMeshCollider()
    {
        meshCollider.sharedMesh = meshFilter.mesh;
    }

    protected void AddVoxelFaceToMesh(int x, int y, int z, int faceId)
    {
        AddVoxelFaceToMesh(new Vector3(x, y, z), faceId);
    }

    protected virtual void AddVoxelFaceToMesh(Vector3 position, int faceId)
    {
        int blockID = GetVoxelId((int)position.x, (int)position.y, (int)position.z);
        AddVoxelFaceToMesh(position, blockID, faceId);
    }

    protected void AddVoxelFaceToMesh(Vector3 position, int blockID, int faceId)
    {
        for (int j = 0; j < 4; j++)
        {
            vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[faceId, j]]);
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

    public virtual Entity GetVoxel(int3 position) { return Entity.Null; }

    protected virtual int GetVoxelId(int x, int y, int z)
    {
        return voxelMap[x, y, z];
    }

    protected virtual void AddvoxelsToMesh()
    {
        for (int y = 0; y < Size.y; y++)
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    if (IsVoxelSolid(x, y, z))
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (CheckNeighbourVoxel(x, y, z, i))
                            {
                                AddVoxelFaceToMesh(x, y, z, i);
                            }
                        }
                    } 
                }
            }
        }
    }

    protected virtual bool IsVoxelSolid(int x, int y, int z)
    {
        return true;
    }

    protected virtual bool CheckNeighbourVoxel(int x, int y, int z, int i)
    {
        return true;
    }

    public virtual void GenerateMeshWithJobs()
    {
        DrawChunk();
    }

    public virtual void GenerateMeshWithJobsGetData()
    {

    }
}
