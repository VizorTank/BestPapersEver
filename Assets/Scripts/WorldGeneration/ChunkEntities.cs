using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ChunkEntities : ChunkCore
{
    //public static ChunkData chunkData;
    //[SerializeField] public static Vector3Int Size = new Vector3Int(32, 32, 32);
    public List<BlockType> blockTypes;
    public Entity[,,] voxels = new Entity[Size.x, Size.y, Size.z];

    [SerializeField] private GameObject gameObjectPrefab;

    private EntityArchetype blockArchetype;
    private EntityArchetype chunkArchetype;

    private Entity entityPrefab;
    private World defaultWorld;
    private Entity chunkEntity;
    private EntityManager entityManager;

    private NativeArray<BlockVisibleSidesData> blockVisibleSidesArray;
    private NativeArray<BlockIdData> blockIdDataArray;
    private NativeArray<Translation> blockTranslation;
    private NativeArray<BlockIsSolidData> blockIsSolidDataArray;

    // Back Front Top Bottom Left Right
    public static int3[] Neighbours = new int3[]
    {
        new int3(0, 0, -1),
        new int3(0, 0, 1),
        new int3(0, 1, 0),
        new int3(0, -1, 0),
        new int3(-1, 0, 0),
        new int3(1, 0, 0),
    };

    public static EntityQueryDesc visibleBlocksQueryDesc = new EntityQueryDesc()
    {
        All = new ComponentType[]
        {
            ComponentType.ReadOnly<BlockVisibleSidesData>(),
            ComponentType.ReadOnly<BlockIsSolidData>(),
            ComponentType.ReadOnly<BlockIdData>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<BlockIsVisibleTag>(),
            ComponentType.ReadOnly<BlockParentChunkData>()
        }
    };

    public ChunkEntities(Vector3Int _position, WorldClass _world) : base(_position, _world)
    {
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;

        //gameObjectPrefab = world.blockEntityPrefab;

        //GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        //entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(gameObjectPrefab, settings);

        blockArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(BlockIdData),
            typeof(BlockIsSolidData),
            typeof(BlockNeighboursData),
            typeof(BlockVisibleSidesData),
            typeof(BlockIsVisibleTag),
            typeof(BlockGenerateDataTag),
            typeof(BlockRequireUpdateTag),
            typeof(BlockParentChunkData)
            );

        chunkArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(ChunkRequirePopulateTag));

        chunkEntity = InstatiateEntity(ChunkPosition, chunkArchetype);

        //GenerateMap();
        //LinkBlocks();
        List<BlockParentChunkData> g = new List<BlockParentChunkData>();
        entityManager.GetAllUniqueSharedComponentData<BlockParentChunkData>(g);
        //Debug.Log(g.Count + g[g.Count - 1].Value.ToString());
        CreateMeshFromEntities();
    }

    public struct GenerateMeshJob : IJob
    {
        [ReadOnly] public NativeArray<BlockVisibleSidesData> blockVisibleSidesDatas;
        [ReadOnly] public NativeArray<BlockIsSolidData> blockIsSolidDatas;
        [ReadOnly] public NativeArray<BlockIdData> blockIdDatas;
        [ReadOnly] public NativeArray<Translation> translations;

        public int vertexIndex;
        public NativeArray<int> trianglesIndexes;
        public NativeArray<float3> vertices;
        public NativeArray<int> triangles;
        public NativeArray<float2> uvs;

        public static int3[] neighbours = ChunkEntities.Neighbours;
        [ReadOnly] public int3 chunkPos;
        [ReadOnly] public int blockCount;

        public void Execute()
        {
            for (int i = 0; i < blockIdDatas.Length; i++)
            {
                if (blockIsSolidDatas[i].Value)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        if (blockIsSolidDatas[i].Value)
                        {
                            int blockID = blockIdDatas[i].Value;
                            float3 position = translations[i].Value - chunkPos;
                            for (int k = 0; k < 4; k++)
                            {
                                vertices[vertexIndex + k] = position + new float3(VoxelData.voxelVerts[VoxelData.voxelTris[j, k]]);
                                uvs[vertexIndex + k] = VoxelData.voxelUvs[k];
                            }
                            
                            triangles[blockID + trianglesIndexes[blockID]++ * blockCount] = vertexIndex + 0;
                            triangles[blockID + trianglesIndexes[blockID]++ * blockCount] = vertexIndex + 1;
                            triangles[blockID + trianglesIndexes[blockID]++ * blockCount] = vertexIndex + 2;
                            triangles[blockID + trianglesIndexes[blockID]++ * blockCount] = vertexIndex + 2;
                            triangles[blockID + trianglesIndexes[blockID]++ * blockCount] = vertexIndex + 1;
                            triangles[blockID + trianglesIndexes[blockID]++ * blockCount] = vertexIndex + 3;
                            //triangles[blockID].Add(vertexIndex + 0);
                            //triangles[blockID].Add(vertexIndex + 1);
                            //triangles[blockID].Add(vertexIndex + 2);
                            //triangles[blockID].Add(vertexIndex + 2);
                            //triangles[blockID].Add(vertexIndex + 1);
                            //triangles[blockID].Add(vertexIndex + 3);
                            vertexIndex += 4;
                        }
                    }
                }
            }
        }
    }

    public override void GenerateMeshWithJobs()
    {
        EntityQuery entityQuery = entityManager.CreateEntityQuery(visibleBlocksQueryDesc);
        entityQuery.SetSharedComponentFilter(new BlockParentChunkData { Value = chunkEntity });

        if (entityQuery.CalculateEntityCount() > 0)
            GenerateMeshWithJobsGetData(entityQuery);
    }

    private void GenerateMeshWithJobsGetData(EntityQuery entityQuery)
    {
        NativeArray<BlockVisibleSidesData> blockVisibleSidesDatas = entityQuery.ToComponentDataArray<BlockVisibleSidesData>(Allocator.TempJob);
        NativeArray<BlockIsSolidData> blockIsSolidDatas = entityQuery.ToComponentDataArray<BlockIsSolidData>(Allocator.TempJob);
        NativeArray<BlockIdData> blockIdDatas = entityQuery.ToComponentDataArray<BlockIdData>(Allocator.TempJob);
        NativeArray<Translation> translations = entityQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        int blockExpectedCount = entityQuery.CalculateEntityCount() * 6;

        NativeArray<int> triangles = new NativeArray<int>(world.blockTypes.Length * blockExpectedCount * 6, Allocator.TempJob);
        NativeArray<int> trianglesIndexes = new NativeArray<int>(world.blockTypes.Length, Allocator.TempJob);

        NativeArray<float3> vertices = new NativeArray<float3>(blockExpectedCount * 4, Allocator.TempJob);
        NativeArray<float2> uvs = new NativeArray<float2>(blockExpectedCount * 4, Allocator.TempJob);

        GenerateMeshJob generateMeshJob = new GenerateMeshJob
        {
            chunkPos = new int3(ChunkPosition),
            blockCount = world.blockTypes.Length,
            blockIdDatas = blockIdDatas,
            blockVisibleSidesDatas = blockVisibleSidesDatas,
            blockIsSolidDatas = blockIsSolidDatas,
            translations = translations,
            triangles = triangles,
            trianglesIndexes = trianglesIndexes,
            uvs = uvs,
            vertices = vertices
        };

        JobHandle jobHandle = generateMeshJob.Schedule();
        jobHandle.Complete();

        blockVisibleSidesDatas.Dispose();
        blockIsSolidDatas.Dispose();
        blockIdDatas.Dispose();
        translations.Dispose();

        List<List<int>> tri = new List<List<int>>();

        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            tri.Add(new List<int>());
            for (int j = 0; j < trianglesIndexes[i]; j++)
            {
                tri[i].Add(triangles[i + j * world.blockTypes.Length]);
            }
        }

        triangles.Dispose();
        trianglesIndexes.Dispose();

        CreateMesh(tri, vertices, uvs);
    }
    protected void CreateMesh(List<List<int>> triangles, NativeArray<float3> vertices, NativeArray<float2> uvs)
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.Reinterpret<Vector3>().ToArray(),
            uv = uvs.Reinterpret<Vector2>().ToArray(),
            subMeshCount = world.blockTypes.Length
        };

        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            mesh.SetTriangles(triangles[i].ToArray(), i, true, 0);
        }

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        List<Material> materials = new List<Material>();
        foreach (var item in world.blockTypes)
        {
            materials.Add(item.material);
        }
        meshRenderer.materials = materials.ToArray();

        vertices.Dispose();
        uvs.Dispose();
    }

    private void PrepareData()
    {
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<BlockVisibleSidesData>(), 
                ComponentType.ReadOnly<BlockParentChunkData>(), 
                ComponentType.ReadOnly<BlockIsSolidData>(),
                ComponentType.ReadOnly<BlockIdData>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<BlockIsVisibleTag>()
            }
        };

        EntityQuery entityQuery = entityManager.CreateEntityQuery(entityQueryDesc);

        entityQuery.SetSharedComponentFilter(new BlockParentChunkData { Value = chunkEntity });

        blockVisibleSidesArray = entityQuery.ToComponentDataArray<BlockVisibleSidesData>(Allocator.Temp);
        blockIdDataArray = entityQuery.ToComponentDataArray<BlockIdData>(Allocator.Temp);
        blockIsSolidDataArray = entityQuery.ToComponentDataArray<BlockIsSolidData>(Allocator.Temp);
        blockTranslation = entityQuery.ToComponentDataArray<Translation>(Allocator.Temp);
    }

    public override void DrawChunk()
    {
        base.DrawChunkBefore();
        PrepareData();
        AddvoxelsToMesh();
        base.DrawChunkAfter();
    }

    protected override void AddvoxelsToMesh()
    {
        for (int i = 0; i < blockVisibleSidesArray.Length; i++)
        {
            if (blockIsSolidDataArray[i].Value)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (CheckNeighbourVoxel(i, 0, 0, j))
                    {
                        AddVoxelFaceToMesh(blockTranslation[i].Value - new float3(ChunkPosition), blockIdDataArray[i].Value, j);
                        //AddVoxelFaceToMesh(blockTranslation[i].Value - new float3(ChunkPosition), blockIdDataArray[i].Value, j);
                    }
                }
            }
        }
    }

    protected override bool IsVoxelSolid(int x, int y, int z)
    {
        int index = z + (y + x * Size.y) * Size.z;
        return blockIsSolidDataArray[index].Value;
    }

    protected override bool CheckNeighbourVoxel(int x, int y, int z, int i)
    {
        // Back Front Top Bottom Left Right
        //BlockVisibleSidesData blockVisibleSides = entityManager.GetComponentData<BlockVisibleSidesData>(voxels[x, y, z]);

        int index = x + (y + z * Size.y) * Size.x;
        // Back Front Top Bottom Left Right
        BlockVisibleSidesData blockVisibleSides = blockVisibleSidesArray[index];

        return blockVisibleSides[i];
    }

    protected override int GetVoxelId(int x, int y, int z)
    {
        return blockIdDataArray[z].Value;
    }

    public void LinkBlocks()
    {
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    LinkBlock(new int3(x, y, z));
                }
            }
        }
    }

    public void LinkBlock(int3 position)
    {
        // Back Front Top Bottom Left Right
        BlockNeighboursData neighbours = new BlockNeighboursData();

        for (int i = 0; i < 6; i++)
        {
            neighbours[i] = GetVoxel(position + Neighbours[i]);
        }
        
        entityManager.SetComponentData(voxels[position.x, position.y, position.z], neighbours);
        
        /*
        DynamicBuffer<BlockNeighbourBufferElement> blockNeighbourBufferElements = entityManager.GetBuffer<BlockNeighbourBufferElement>(
                voxels[position.x, position.y, position.z]);

        for (int i = 0; i < 6; i++)
        {
            blockNeighbourBufferElements.Add(new BlockNeighbourBufferElement { Value = neighbours[i] });
        }
        */
    }

    public override Entity GetVoxel(int3 position)
    {
        if (position.x < 0 || position.x >= Size.x ||
            position.y < 0 || position.y >= Size.y ||
            position.z < 0 || position.z >= Size.z)
        {
            return world.GetVoxelEntity(position + new float3(ChunkPosition));
        }
        else
        {
            return voxels[position.x, position.y, position.z];
        }
    }

    public void GenerateMap()
    {
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    voxels[x, y, z] = InstatiateEntity(new float3(x, y, z) + new float3(ChunkPosition));
                    entityManager.SetSharedComponentData(voxels[x, y, z], new BlockParentChunkData { Value = chunkEntity });
                }
            }
        }
    }

    private Entity InstatiateEntity(float3 position)
    {
        return InstatiateEntity(position, blockArchetype);
    }

    private Entity InstatiateEntity(float3 position, EntityArchetype entityArchetype)
    {
        //Entity entity = entityManager.Instantiate(entityPrefab);
        Entity entity = entityManager.CreateEntity(entityArchetype);
        entityManager.SetComponentData(entity, new Translation { Value = position });
        return entity;
    }

    public int GetBlockIdDataIndex(int x, int y, int z)
    {
        return z + (y + x * Size.y) * Size.z;
    }
    public int GenerateVoxel(int x, int y, int z)
    {
        if (y > 5)
            return 0;
        return 1;
    }
}
