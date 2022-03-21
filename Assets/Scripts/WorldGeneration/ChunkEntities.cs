using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
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

    private GenerateMeshJob generateMeshJob;
    private JobHandle generateMeshRaw;
    private List<GenerateMeshListJob> generateMeshListJobs;
    private JobHandle generateMeshListDepedency;
    private bool requireUpdate;
    private bool requireMeshLists;

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

    public static EntityQueryDesc chunkRequireUpdate = new EntityQueryDesc()
    {
        All = new ComponentType[]
        {
            ComponentType.ReadOnly<ChunkRequireUpdateTag>()
        }
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

    //[BurstCompile]
    public struct GenerateMeshJob : IJob
    {
        [ReadOnly] public NativeArray<BlockVisibleSidesData> blockVisibleSidesDatas;
        [ReadOnly] public NativeArray<BlockIsSolidData> blockIsSolidDatas;
        [ReadOnly] public NativeArray<BlockIdData> blockIdDatas;
        [ReadOnly] public NativeArray<Translation> translations;

        public int vertexIndex;
        public NativeArray<float3> vertices;
        public NativeArray<float2> uvs;

        public NativeArray<int> blockIdCounts;

        public int triangleBlockIdIndex;
        public NativeArray<int> triangleBlockIds;
        public int triangleIndex;
        public NativeArray<int> triangles;

        public static int3[] neighbours = ChunkEntities.Neighbours;
        [ReadOnly] public float3 chunkPos;
        [ReadOnly] public int blockCount;

        public void Execute()
        {
            for (int i = 0; i < blockIdDatas.Length; i++)
            {
                if (blockIsSolidDatas[i].Value)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        if (blockVisibleSidesDatas[i][j])
                        {
                            int blockID = blockIdDatas[i].Value;
                            float3 position = translations[i].Value - chunkPos;
                            for (int k = 0; k < 4; k++)
                            {
                                vertices[vertexIndex + k] = position + new float3(VoxelData.voxelVerts[VoxelData.voxelTris[j, k]]);
                                uvs[vertexIndex + k] = VoxelData.voxelUvs[k];
                            }
                            blockIdCounts[blockID]++;
                            triangleBlockIds[triangleBlockIdIndex++] = blockID;
                            triangles[triangleIndex++] = vertexIndex + 0;
                            triangles[triangleIndex++] = vertexIndex + 1;
                            triangles[triangleIndex++] = vertexIndex + 2;
                            blockIdCounts[blockID]++;
                            triangleBlockIds[triangleBlockIdIndex++] = blockID;
                            triangles[triangleIndex++] = vertexIndex + 2;
                            triangles[triangleIndex++] = vertexIndex + 1;
                            triangles[triangleIndex++] = vertexIndex + 3;
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

    [BurstCompile]
    public struct GenerateMeshListJob : IJob
    {
        [ReadOnly] public NativeArray<int> triangles;
        [ReadOnly] public NativeArray<int> triangleBlockIds;

        [ReadOnly] public int blockId;
        [ReadOnly] public int blockMaxCount;

        private int filteredTrianglesIndex;
        public NativeArray<int> filteredTriangles;
        public void Execute()
        {
            if (blockId == 0)
                return;
            for (int i = 0; i < triangleBlockIds.Length; i++)
            {
                if (triangleBlockIds[i] == blockId)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        //if (filteredTrianglesIndex >= blockMaxCount)
                        //{
                        //    Debug.LogError("Too many blocks in blockID: " + blockId + " Count: " + filteredTrianglesIndex + " Max: " + blockMaxCount);
                        //    continue;
                        //}

                        filteredTriangles[filteredTrianglesIndex++] = triangles[i * 3 + j];
                    }
                }
            }
        }
    }

    public override void GenerateMeshWithJobs()
    {
        if (requireUpdate)
            return;

        EntityQuery chunkUpdates = entityManager.CreateEntityQuery(chunkRequireUpdate);
        NativeArray<Entity> chunks = chunkUpdates.ToEntityArray(Allocator.Temp);

        if (chunks.BinarySearch(chunkEntity) > 0)
        {
            chunks.Dispose();
            EntityQuery entityQuery = entityManager.CreateEntityQuery(visibleBlocksQueryDesc);
            entityQuery.SetSharedComponentFilter(new BlockParentChunkData { Value = chunkEntity });

            if (entityQuery.CalculateEntityCount() > 0)
            {
                requireUpdate = true;
                requireMeshLists = true;
                PrepareMeshWithJobsGetData(entityQuery);
            }

            entityManager.RemoveComponent<ChunkRequireUpdateTag>(chunkEntity);
        }
    }

    private void PrepareMeshWithJobsGetData(EntityQuery entityQuery)
    {
        NativeArray<BlockVisibleSidesData> blockVisibleSidesDatas = entityQuery.ToComponentDataArray<BlockVisibleSidesData>(Allocator.TempJob);
        NativeArray<BlockIsSolidData> blockIsSolidDatas = entityQuery.ToComponentDataArray<BlockIsSolidData>(Allocator.TempJob);
        NativeArray<BlockIdData> blockIdDatas = entityQuery.ToComponentDataArray<BlockIdData>(Allocator.TempJob);
        NativeArray<Translation> translations = entityQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        int blockExpectedCount = entityQuery.CalculateEntityCount() * 6;

        NativeArray<int> blockIdCount = new NativeArray<int>(world.blockTypes.Length, Allocator.TempJob);

        NativeArray<int> triangles = new NativeArray<int>(blockExpectedCount * 6, Allocator.TempJob);
        NativeArray<int> triangleBlockIds = new NativeArray<int>(blockExpectedCount * 2, Allocator.TempJob);
        //NativeArray<int> trianglesIndexes = new NativeArray<int>(world.blockTypes.Length, Allocator.TempJob);

        NativeArray<float3> vertices = new NativeArray<float3>(blockExpectedCount * 4, Allocator.TempJob);
        NativeArray<float2> uvs = new NativeArray<float2>(blockExpectedCount * 4, Allocator.TempJob);

        generateMeshJob = new GenerateMeshJob
        {
            chunkPos = new float3(ChunkPosition),
            blockCount = world.blockTypes.Length,

            blockIdDatas = blockIdDatas,
            blockVisibleSidesDatas = blockVisibleSidesDatas,
            blockIsSolidDatas = blockIsSolidDatas,
            translations = translations,

            blockIdCounts = blockIdCount,

            triangles = triangles,
            triangleBlockIds = triangleBlockIds,

            uvs = uvs,
            vertices = vertices
        };

        generateMeshRaw = generateMeshJob.Schedule();
    }

    public void PlanGeneratingMeshLists()
    {
        generateMeshRaw.Complete();
        generateMeshListJobs = new List<GenerateMeshListJob>(world.blockTypes.Length);
        NativeArray<JobHandle> generateMeshListJobHandles = new NativeArray<JobHandle>(world.blockTypes.Length, Allocator.TempJob);

        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            generateMeshListJobs.Add(new GenerateMeshListJob
            {
                triangles = generateMeshJob.triangles,
                triangleBlockIds = generateMeshJob.triangleBlockIds,
                blockMaxCount = generateMeshJob.blockIdCounts[i] * 3,

                blockId = i,
                filteredTriangles = new NativeArray<int>(generateMeshJob.blockIdCounts[i] * 3, Allocator.TempJob)
            });

            generateMeshListJobHandles[i] = generateMeshListJobs[i].Schedule(generateMeshRaw);
        }

        generateMeshListDepedency = JobHandle.CombineDependencies(generateMeshListJobHandles);
        generateMeshListJobHandles.Dispose(generateMeshListDepedency);
    }

    public override void GenerateMeshWithJobsGetData()
    {
        if (!requireUpdate || !generateMeshRaw.IsCompleted)
            return;

        generateMeshRaw.Complete();

        if (requireMeshLists)
        {
            PlanGeneratingMeshLists();
            requireMeshLists = false;
        }

        if (!generateMeshListDepedency.IsCompleted)
            return;

        generateMeshListDepedency.Complete();


        List<int[]> tri = new List<int[]>(world.blockTypes.Length);

        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            tri.Add(generateMeshListJobs[i].filteredTriangles.ToArray());
            generateMeshListJobs[i].filteredTriangles.Dispose(generateMeshListDepedency);
        }

        generateMeshJob.blockVisibleSidesDatas.Dispose(generateMeshRaw);
        generateMeshJob.blockIsSolidDatas.Dispose(generateMeshRaw);
        generateMeshJob.blockIdDatas.Dispose(generateMeshRaw);
        generateMeshJob.translations.Dispose(generateMeshRaw);

        generateMeshJob.blockIdCounts.Dispose(generateMeshRaw);

        generateMeshJob.triangleBlockIds.Dispose(generateMeshRaw);
        generateMeshJob.triangles.Dispose(generateMeshRaw);

        CreateMesh(tri, generateMeshJob.vertices, generateMeshJob.uvs);
        requireUpdate = false;
    }

    protected void CreateMesh(List<int[]> triangles, NativeArray<float3> vertices, NativeArray<float2> uvs)
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.Reinterpret<Vector3>().ToArray(),
            uv = uvs.Reinterpret<Vector2>().ToArray(),
            subMeshCount = world.blockTypes.Length
        };

        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            mesh.SetTriangles(triangles[i], i, true, 0);
            //triangles[i].Dispose(generateMeshListDepedency);
        }

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        List<Material> materials = new List<Material>();
        foreach (var item in world.blockTypes)
        {
            materials.Add(item.material);
        }
        meshRenderer.materials = materials.ToArray();

        //triangles.Dispose(generateMeshListDepedency);
        vertices.Dispose(generateMeshListDepedency);
        uvs.Dispose(generateMeshListDepedency);
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
