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

    private NativeArray<int> triangleOrder;

    // Back Front Top Bottom Left Right
    private NativeArray<int3> voxelNeighbours;
    private NativeArray<float3> voxelVerts;
    private int voxelTrisSize;
    private NativeArray<int> voxelTris;
    private NativeArray<float2> voxelUvs;
    private NativeArray<VertexAttributeDescriptor> layout;

    private bool requireUpdate;

    private Mesh.MeshDataArray meshDataArray;
    private CreateMeshJob createMeshJob;
    private JobHandle createMeshJobHandle;

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
        layout = new NativeArray<VertexAttributeDescriptor>(VoxelDataV2.layoutVertex, Allocator.Persistent);
        triangleOrder = new NativeArray<int>(VoxelDataV2.triangleOrder, Allocator.Persistent);
    }

    public void Destroy()
    {
        voxelNeighbours.Dispose();
        voxelVerts.Dispose();
        voxelUvs.Dispose();
        voxelTris.Dispose();
        layout.Dispose();
        triangleOrder.Dispose();
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
                PrepareMeshWithJobsGetData2(entityQuery);
            }

            entityManager.RemoveComponent<ChunkRequireUpdateTag>(chunkEntity);
        }
    }

    private void PrepareMeshWithJobsGetData2(EntityQuery entityQuery)
    {
        // Get data to process
        NativeArray<BlockVisibleSidesData> blockVisibleSidesDatas = entityQuery.ToComponentDataArray<BlockVisibleSidesData>(Allocator.TempJob);
        NativeArray<BlockIsSolidData> blockIsSolidDatas = entityQuery.ToComponentDataArray<BlockIsSolidData>(Allocator.TempJob);
        NativeArray<BlockIdData> blockIdDatas = entityQuery.ToComponentDataArray<BlockIdData>(Allocator.TempJob);
        NativeArray<Translation> translations = entityQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        // Allocate mesh to create
        meshDataArray = Mesh.AllocateWritableMeshData(1);

        createMeshJob = new CreateMeshJob
        {
            // Data
            blockIdDatas = blockIdDatas,
            blockVisibleSidesDatas = blockVisibleSidesDatas,
            blockIsSolidDatas = blockIsSolidDatas,
            translations = translations,

            // Const
            neighbours = voxelNeighbours,
            voxelTris = voxelTris,
            voxelTrisSize = voxelTrisSize,
            voxelUvs = voxelUvs,
            voxelVerts = voxelVerts,

            triangleOrder = triangleOrder,

            // Block Types Count
            blockTypesCount = world.blockTypes.Length,

            // How data is inserted to MeshData
            layout = layout,

            // Chunk position
            chunkPos = new float3(ChunkPosition),

            // Mesh to create
            data = meshDataArray[0]
        };

        // Schedule job
        createMeshJobHandle = createMeshJob.Schedule();
    }

    [BurstCompile]
    public struct CreateMeshJob : IJob
    {
        // Input data
        [ReadOnly] public NativeArray<BlockVisibleSidesData> blockVisibleSidesDatas;
        [ReadOnly] public NativeArray<BlockIsSolidData> blockIsSolidDatas;
        [ReadOnly] public NativeArray<BlockIdData> blockIdDatas;
        [ReadOnly] public NativeArray<Translation> translations;

        // Static values
        // Block attributes
        [ReadOnly] public NativeArray<int3> neighbours;
        [ReadOnly] public NativeArray<float3> voxelVerts;
        [ReadOnly] public NativeArray<int> voxelTris;
        [ReadOnly] public int voxelTrisSize;
        [ReadOnly] public NativeArray<float2> voxelUvs;

        // Order of vertices of triangles on side
        [ReadOnly] public NativeArray<int> triangleOrder;

        // Block type count
        [ReadOnly] public int blockTypesCount;

        // Chunk position
        [ReadOnly] public float3 chunkPos;

        // How data is inserted to MeshData
        [ReadOnly] public NativeArray<VertexAttributeDescriptor> layout;

        // Output mesh
        public Mesh.MeshData data;

        public void Execute()
        {
            // Count sides and sides per block type
            NativeArray<int> sidesCountPerType = new NativeArray<int>(blockTypesCount, Allocator.Temp);
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
                            sidesCountPerType[blockIdDatas[i].Value]++;
                        }
                    }
                }
            }

            // Table of sums of previous triangles
            NativeArray<int> trianglesCountPerTypeSum = new NativeArray<int>(blockTypesCount, Allocator.Temp);
            int sumOfPreviousTriangles = 0;
            for (int i = 0; i < blockTypesCount; i++)
            {
                trianglesCountPerTypeSum[i] = sumOfPreviousTriangles;
                sumOfPreviousTriangles += sidesCountPerType[i] * 6;
            }

            // Set data type and size for MeshData
            data.SetVertexBufferParams(sidesCount * 4, layout);
            NativeArray<VertexPositionUvStruct> vertex = data.GetVertexData<VertexPositionUvStruct>();

            // Set index type and size for MeshData
            data.SetIndexBufferParams(sidesCount * 6, IndexFormat.UInt32);
            NativeArray<int> indexes = data.GetIndexData<int>();

            // Indexes per Block Type
            NativeArray<int> triangleIndexes = new NativeArray<int>(blockTypesCount, Allocator.Temp);
            int vertexIndex = 0;

            // Foreach block
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
                            for (int k = 0; k < 6; k++)
                            {
                                indexes[trianglesCountPerTypeSum[blockID] + triangleIndexes[blockID]++] = vertexIndex + triangleOrder[k];
                            }
                            vertexIndex += 4;
                        }
                    }
                }
            }

            data.subMeshCount = blockTypesCount;
            for (int i = 0; i < blockTypesCount; i++)
            {
                data.SetSubMesh(i, new SubMeshDescriptor(trianglesCountPerTypeSum[i], triangleIndexes[i]));
            }

            trianglesCountPerTypeSum.Dispose();
            triangleIndexes.Dispose();
            sidesCountPerType.Dispose();
        }
    }

    public void GenerateMeshWithJobsGetData2()
    {
        if (!requireUpdate || !createMeshJobHandle.IsCompleted)
            return;

        requireUpdate = false;

        createMeshJobHandle.Complete();

        Mesh mesh = new Mesh();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        meshRenderer.materials = world.materials.ToArray();

        createMeshJob.blockVisibleSidesDatas.Dispose();
        createMeshJob.blockIsSolidDatas.Dispose();
        createMeshJob.blockIdDatas.Dispose();
        createMeshJob.translations.Dispose();
    }

    private Entity InstatiateEntity(float3 position, EntityArchetype entityArchetype)
    {
        Entity entity = entityManager.CreateEntity(entityArchetype);
        entityManager.SetComponentData(entity, new Translation { Value = position });
        return entity;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct VertexPositionUvStruct
    {
        public float3 pos;
        public float2 uv;
    }
}
