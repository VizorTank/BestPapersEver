using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ChunkV4
{
    private Vector3Int coordinates;
    private WorldClassV2 world;

    private GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private World defaultWorld;
    private Entity chunkEntity;
    private EntityManager entityManager;

    public static int cubeSize = 16;
    public static int3 Size = new int3(cubeSize, cubeSize, cubeSize);

    // Back Front Top Bottom Left Right
    private static int3[] Neighbours = new int3[]
    {
        new int3(0, 0, -1),
        new int3(0, 0, 1),
        new int3(0, 1, 0),
        new int3(0, -1, 0),
        new int3(-1, 0, 0),
        new int3(1, 0, 0),
    };

    private static EntityQueryDesc chunkRequireUpdate = new EntityQueryDesc()
    {
        All = new ComponentType[]
        {
            ComponentType.ReadOnly<ChunkRequireUpdateTag>()
        }
    };

    private static EntityQueryDesc visibleBlocksQueryDesc = new EntityQueryDesc()
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

    // Maybe Delete this
    private bool requireUpdate;
    private bool requireMeshLists;
    private GenerateMeshJob generateMeshJob;
    private JobHandle generateMeshRaw;
    private List<GenerateMeshListJob> generateMeshListJobs;
    private JobHandle generateMeshListDepedency;

    public float3 ChunkPosition
    {
        get { return new float3(coordinates.x, coordinates.y, coordinates.z) * Size; }
    }

    public ChunkV4(Vector3Int _position, WorldClassV2 _world)
    {
        coordinates = _position;
        world = _world;

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = Vector3.Scale(coordinates, new Vector3(Size.x, Size.y, Size.z));
        chunkObject.name = string.Format("Chunk {0}, {1}, {2}", coordinates.x, coordinates.y, coordinates.z);

        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        EntityArchetype chunkArchetype = entityManager.CreateArchetype(
                    typeof(Translation),
                    typeof(Rotation),
                    typeof(MeshFilter),
                    typeof(MeshRenderer),
                    typeof(ChunkRequirePopulateTag));
        chunkEntity = InstatiateEntity(ChunkPosition, chunkArchetype);
    }

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

        public static int3[] neighbours = Neighbours;
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
                                // TODO: Remove VoxelData
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

    public void GenerateMeshWithJobs()
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

    public void GenerateMeshWithJobsGetData()
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

    private Entity InstatiateEntity(float3 position, EntityArchetype entityArchetype)
    {
        //Entity entity = entityManager.Instantiate(entityPrefab);
        Entity entity = entityManager.CreateEntity(entityArchetype);
        entityManager.SetComponentData(entity, new Translation { Value = position });
        return entity;
    }
}
