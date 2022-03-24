using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

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

    // Back Front Top Bottom Left Right
    private NativeArray<int3> voxelNeighbours;
    private NativeArray<float3> voxelVerts;
    private int voxelTrisSize;
    private NativeArray<int> voxelTris;
    private NativeArray<float2> voxelUvs;

    // Maybe Delete this
    private bool requireUpdate;
    private bool requireMeshLists;
    private GenerateMeshJob generateMeshJob;
    private JobHandle generateMeshRaw;
    private List<GenerateMeshListJob> generateMeshListJobs;
    private JobHandle generateMeshListDepedency;

    // Version 2
    private CreateMeshJob createMeshJob;
    private JobHandle createHeshJobHandle;

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

        voxelNeighbours = new NativeArray<int3>(VoxelDataV2.voxelNeighbours, Allocator.Persistent);
        voxelVerts = new NativeArray<float3>(VoxelDataV2.voxelVerts, Allocator.Persistent);
        voxelUvs = new NativeArray<float2>(VoxelDataV2.voxelUvs, Allocator.Persistent);
        voxelTris = new NativeArray<int>(VoxelDataV2.voxelTris, Allocator.Persistent);
        voxelTrisSize = VoxelDataV2.voxelTrisSize;
    }

    ~ChunkV4()
    {
        voxelNeighbours.Dispose();
        voxelVerts.Dispose();
        voxelUvs.Dispose();
        voxelTris.Dispose();
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

        [ReadOnly] public NativeArray<int3> neighbours;
        [ReadOnly] public NativeArray<float3> voxelVerts;
        [ReadOnly] public NativeArray<int> voxelTris;
        [ReadOnly] public int voxelTrisSize;
        [ReadOnly] public NativeArray<float2> voxelUvs;
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
                                vertices[vertexIndex + k] = position + new float3(voxelVerts[voxelTris[j * voxelTrisSize + k]]);
                                uvs[vertexIndex + k] = voxelUvs[k];
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

            neighbours = voxelNeighbours,
            voxelTris = voxelTris,
            voxelTrisSize = voxelTrisSize,
            voxelUvs = voxelUvs,
            voxelVerts = voxelVerts,

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

    private void PrepareMeshWithJobsGetData2(EntityQuery entityQuery)
    {
        NativeArray<BlockVisibleSidesData> blockVisibleSidesDatas = entityQuery.ToComponentDataArray<BlockVisibleSidesData>(Allocator.TempJob);
        NativeArray<BlockIsSolidData> blockIsSolidDatas = entityQuery.ToComponentDataArray<BlockIsSolidData>(Allocator.TempJob);
        NativeArray<BlockIdData> blockIdDatas = entityQuery.ToComponentDataArray<BlockIdData>(Allocator.TempJob);
        NativeArray<Translation> translations = entityQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        createMeshJob = new CreateMeshJob
        {
            chunkPos = new float3(ChunkPosition),
            blockTypesCount = world.blockTypes.Length,

            neighbours = voxelNeighbours,
            voxelTris = voxelTris,
            voxelTrisSize = voxelTrisSize,
            voxelUvs = voxelUvs,
            voxelVerts = voxelVerts,

            blockIdDatas = blockIdDatas,
            blockVisibleSidesDatas = blockVisibleSidesDatas,
            blockIsSolidDatas = blockIsSolidDatas,
            translations = translations
        };

        createHeshJobHandle = createMeshJob.Schedule();
    }

    public struct CreateMeshJob : IJob
    {
        // Input data
        [ReadOnly] public NativeArray<BlockVisibleSidesData> blockVisibleSidesDatas;
        [ReadOnly] public NativeArray<BlockIsSolidData> blockIsSolidDatas;
        [ReadOnly] public NativeArray<BlockIdData> blockIdDatas;
        [ReadOnly] public NativeArray<Translation> translations;

        //public int vertexIndex;
        //public NativeArray<float3> vertices;
        //public NativeArray<float2> uvs;

        //public NativeArray<int> blockIdCounts;

        //public int triangleBlockIdIndex;
        //public NativeArray<int> triangleBlockIds;
        //public int triangleIndex;
        //public NativeArray<int> triangles;

        // Static values
        // Block attributes
        [ReadOnly] public NativeArray<int3> neighbours;
        [ReadOnly] public NativeArray<float3> voxelVerts;
        [ReadOnly] public NativeArray<int> voxelTris;
        [ReadOnly] public int voxelTrisSize;
        [ReadOnly] public NativeArray<float2> voxelUvs;

        // Block type count
        [ReadOnly] public int blockTypesCount;

        // Chunk position
        [ReadOnly] public float3 chunkPos;

        // Output mesh
        public Mesh.MeshData data;

        public void Execute()
        {
            NativeArray<int> blockCountPerType = new NativeArray<int>(blockTypesCount, Allocator.Temp);
            int sidesCount = 0;
            for (int i = 0; i < blockIdDatas.Length; i++)
            {
                if (blockIsSolidDatas[i].Value)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        if (blockVisibleSidesDatas[i][j])
                        {
                            sidesCount++;
                            blockCountPerType[blockIdDatas[i].Value]++;
                        }
                    }
                }
            }

            NativeArray<int> blockCountPerTypeSum = new NativeArray<int>(blockTypesCount, Allocator.Temp);

            int sumOfPreviousBlocksTypes = 0;
            for (int i = 0; i < blockTypesCount; i++)
            {
                blockCountPerTypeSum[i] = sumOfPreviousBlocksTypes;
                sumOfPreviousBlocksTypes += blockCountPerType[i];
            }

            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
            };

            data.SetVertexBufferParams(sidesCount * 4, layout);
            NativeArray<VertexPositionUvStruct> vertex = data.GetVertexData<VertexPositionUvStruct>();

            data.SetIndexBufferParams(sidesCount * 6, IndexFormat.UInt32);
            NativeArray<int> indexes = data.GetIndexData<int>();

            NativeArray<int> triangleIndexes = new NativeArray<int>(blockTypesCount, Allocator.Temp);
            int vertexIndex = 0;
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
                                vertex[vertexIndex + k] = new VertexPositionUvStruct
                                {
                                    pos = position + voxelVerts[voxelTris[j * voxelTrisSize + k]],
                                    uv = voxelUvs[k]
                                };
                            }
                            int[] triangleOrder = new int[] { 0, 1, 2, 2, 1, 3 };
                            for (int k = 0; k < 6; k++)
                            {
                                indexes[blockCountPerTypeSum[blockID] + triangleIndexes[blockID]++] = vertexIndex + triangleOrder[k];
                            }
                            vertexIndex += 4;
                        }
                    }
                }
            }

            data.subMeshCount = blockTypesCount;
            for (int i = 0; i < blockTypesCount; i++)
            {
                data.SetSubMesh(i, new SubMeshDescriptor(blockCountPerTypeSum[i], triangleIndexes[i]));
            }

            blockCountPerTypeSum.Dispose();
            triangleIndexes.Dispose();
            blockCountPerType.Dispose();
        }
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

        CreateMesh2(tri, generateMeshJob.vertices, generateMeshJob.uvs);
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

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct VertexPositionUvStruct
    {
        public float3 pos;
        public float2 uv;
    }

    protected void CreateMesh2(List<int[]> triangles, NativeArray<float3> vertices, NativeArray<float2> uvs)
    {
        Mesh.MeshDataArray dataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData data = dataArray[0];

        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
        };

        data.SetVertexBufferParams(vertices.Length, layout);
        NativeArray<VertexPositionUvStruct> pos = data.GetVertexData<VertexPositionUvStruct>();

        for (int i = 0; i < vertices.Length; i++)
        {
            pos[i] = new VertexPositionUvStruct
            {
                pos = vertices[i],
                uv = uvs[i]
            };
        }

        int triangleCount = 0;
        List<int> tri = new List<int>();
        List<int> meshStartIndex = new List<int>();
        for (int i = 0; i < triangles.Count; i++)
        {
            meshStartIndex.Add(triangleCount);
            triangleCount += triangles[i].Length;
            for (int j = 0; j < triangles[i].Length; j++)
            {
                tri.Add(triangles[i][j]);
            }
        }

        data.SetIndexBufferParams(triangleCount, IndexFormat.UInt32);

        var indexes = data.GetIndexData<int>();
        for (int i = 0; i < triangleCount; i++)
        {
            indexes[i] = tri[i];
        }

        data.subMeshCount = meshStartIndex.Count;
        for (int i = 0; i < meshStartIndex.Count; i++)
        {
            data.SetSubMesh(i, new SubMeshDescriptor(meshStartIndex[i], triangles[i].Length));
        }

        Mesh mesh = new Mesh();

        Mesh.ApplyAndDisposeWritableMeshData(dataArray, mesh);

        //mesh.uv = uvs.Reinterpret<Vector2>().ToArray();

        //for (int i = 0; i < world.blockTypes.Length; i++)
        //{
        //    mesh.SetTriangles(triangles[i], i, true, 0);
        //    //triangles[i].Dispose(generateMeshListDepedency);
        //}

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        
        meshRenderer.materials = world.materials.ToArray();

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
