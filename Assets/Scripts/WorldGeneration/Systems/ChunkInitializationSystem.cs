using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ChunkInitializationSystem : SystemBase
{
    private BeginInitializationEntityCommandBufferSystem BeginEntityCommandBufferSystem;
    private EndInitializationEntityCommandBufferSystem EndEntityCommandBufferSystem;
    private int3 ChunkSize = ChunkV4.Size;

    private NativeArray<Translation> translations;

    // Back Front Top Bottom Left Right
    private int3[] Neighbours = new int3[]
    {
        new int3(0, 0, -1),
        new int3(0, 0, 1),
        new int3(0, 1, 0),
        new int3(0, -1, 0),
        new int3(-1, 0, 0),
        new int3(1, 0, 0),
    };

    private struct GenerateTranslationList : IJobParallelFor
    {
        [ReadOnly] public int3 chunkSize;
        public NativeArray<Translation> translations;
        public void Execute(int index)
        {
            int x = index % chunkSize.x;
            int y = ((index - x) / chunkSize.x) % chunkSize.y;
            int z = (((index - x) / chunkSize.x) - y) / chunkSize.y;

            translations[index] = new Translation { Value = new float3(x, y, z) };
        }
    }

    protected override void OnCreate()
    {
        BeginEntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        EndEntityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();

        //TODO: Create chunk entities prefab in new world and then copy it

        CreateTranslationListOnJobs();
    }

    private void CreateTranslationListOnJobs()
    {
        translations = new NativeArray<Translation>(ChunkSize.x * ChunkSize.y * ChunkSize.z, Allocator.Persistent);

        GenerateTranslationList generateTranslationList = new GenerateTranslationList
        {
            chunkSize = ChunkSize,
            translations = translations
        };

        generateTranslationList.Schedule(translations.Length, 64).Complete();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        translations.Dispose();
    }

    private struct CreateLinkingListJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int3> neighbours;
        [ReadOnly] public int3 chunkSize;

        [ReadOnly] public NativeArray<Entity> blocks;
        public NativeArray<BlockNeighboursData> blockNeighbours;
        public void Execute(int index)
        {
            int idx = index;
            //index = chunkSize.x * chunkSize.y * chunkSize.z - index;
            int x = index % chunkSize.x;
            int y = ((index - x) / chunkSize.x) % chunkSize.y;
            int z = (((index - x) / chunkSize.x) - y) / chunkSize.y;
            int3 pos = new int3(x, y, z);
            BlockNeighboursData neighboursData = new BlockNeighboursData();
            for (int i = 0; i < 6; i++)
            {
                int3 nPos = pos + neighbours[i];
                if (nPos.x < 0 || nPos.x >= chunkSize.x ||
                     nPos.y < 0 || nPos.y >= chunkSize.y ||
                     nPos.z < 0 || nPos.z >= chunkSize.z)
                    continue;
                neighboursData[i] = blocks[nPos.x + (nPos.y + nPos.z * chunkSize.y) * chunkSize.x];
            }
            blockNeighbours[idx] = neighboursData;
        }
    }

    private struct MoveTranslationsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Translation> localTranslations;
        public NativeArray<Translation> globalTranslations;
        public float3 chunkPosition;
        public void Execute(int index)
        {
            globalTranslations[index] = new Translation { Value = chunkPosition + localTranslations[index].Value };
        }
    }

    //private struct LinkBlocksJob : IJob
    //{
    //    [ReadOnly] public NativeArray<int3> neighbours;
    //    [ReadOnly] public NativeArray<Entity> blocks;
    //    [ReadOnly] public int3 chunkSize;
    //    public EntityCommandBuffer.ParallelWriter commandBuffer;
    //    public void Execute()
    //    {
    //        // TODO: Export result as array and SetComponent in batches
    //        for (int x = 0; x < chunkSize.x; x++)
    //        {
    //            for (int y = 0; y < chunkSize.y; y++)
    //            {
    //                for (int z = 0; z < chunkSize.z; z++)
    //                {
    //                    BlockNeighboursData blockNeighboursData = new BlockNeighboursData();
    //                    for (int i = 0; i < 6; i++)
    //                    {
    //                        int3 neighbourPos = new int3(x, y, z) + neighbours[i];
    //                        if (!(neighbourPos.x < 0 || neighbourPos.x >= chunkSize.x ||
    //                                neighbourPos.y < 0 || neighbourPos.y >= chunkSize.y ||
    //                                neighbourPos.z < 0 || neighbourPos.z >= chunkSize.z))
    //                            blockNeighboursData[i] = blocks[neighbourPos.x + (neighbourPos.y + neighbourPos.z * chunkSize.y) * chunkSize.x];
    //                    }
    //                    commandBuffer.SetComponent(0, blocks[x + (y + z * chunkSize.y) * chunkSize.x], blockNeighboursData);
    //                    //commandBuffer.SetComponent(0, blocks[x + (y + z * chunkSize.y) * chunkSize.x], new Translation { Value = new float3(x, y, z) });
    //                }
    //            }
    //        }
    //    }
    //}

    protected override void OnUpdate()
    {
        EntityQuery chunksRequirePopulateQuerry = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ChunkRequirePopulateTag>());
        if (chunksRequirePopulateQuerry.CalculateEntityCount() <= 0)
            return;

        var beginCommandBuffer = BeginEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var endCommandBuffer = EndEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        int3 chunkSize = ChunkSize;
        int chunkBitSize = chunkSize.x * chunkSize.y * chunkSize.z;
        NativeArray<int3> neighbours = new NativeArray<int3>(Neighbours, Allocator.TempJob);

        var blockArchetype = EntityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(BlockIdData),
            typeof(BlockIsSolidData),
            typeof(BlockNeighboursData),
            typeof(BlockVisibleSidesData),
            typeof(BlockIsVisibleTag),
            typeof(BlockGenerateDataTag)//,
            //typeof(BlockRequireUpdateTag)
            );

        var entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<BlockRequirePopulateTag>()
            }
        };

        
        NativeArray<Entity> chunksRequirePopulate = chunksRequirePopulateQuerry.ToEntityArray(Allocator.Temp);

        foreach (Entity chunk in chunksRequirePopulate)
        {
            NativeArray<Entity> blocks = EntityManager.CreateEntity(blockArchetype, chunkBitSize, Allocator.TempJob);
            EntityManager.AddComponent(blocks, ComponentType.ReadOnly<BlockRequirePopulateTag>());

            EntityQuery entityQuery = EntityManager.CreateEntityQuery(entityQueryDesc);
            EntityManager.AddSharedComponentData(entityQuery, new BlockParentChunkData { Value = chunk });

            NativeArray<BlockNeighboursData> blockNeighboursDatas = new NativeArray<BlockNeighboursData>(chunkBitSize, Allocator.TempJob);

            NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.TempJob);

            CreateLinkingListJob job = new CreateLinkingListJob
            {
                blocks = entities,
                chunkSize = chunkSize,
                neighbours = neighbours,
                blockNeighbours = blockNeighboursDatas
            };
            JobHandle jobHandle = job.Schedule(chunkBitSize, 64);

            NativeArray<Translation> chunkTranslations = new NativeArray<Translation>(chunkBitSize, Allocator.TempJob);
            MoveTranslationsJob moveTranslationsJob = new MoveTranslationsJob
            {
                chunkPosition = EntityManager.GetComponentData<Translation>(chunk).Value,
                globalTranslations = chunkTranslations,
                localTranslations = translations
            };
            JobHandle moveTranslationJobHandle = moveTranslationsJob.Schedule(chunkBitSize, 64);

            jobHandle.Complete();
            EntityManager.AddComponentData(entityQuery, blockNeighboursDatas);

            moveTranslationJobHandle.Complete();
            EntityManager.AddComponentData(entityQuery, chunkTranslations);
            
            EntityManager.RemoveComponent<BlockRequirePopulateTag>(entityQuery);
            blockNeighboursDatas.Dispose(jobHandle);
            chunkTranslations.Dispose();
            blocks.Dispose();
            entities.Dispose();
        }

        EntityManager.RemoveComponent<ChunkRequirePopulateTag>(chunksRequirePopulateQuerry);
        chunksRequirePopulate.Dispose();
        neighbours.Dispose();

        //JobHandle jobHandle = Entities
        //    .WithAll<ChunkRequirePopulateTag>()
        //    .ForEach((in Entity entity, in int entityInQueryIndex) =>
        //{
        //    NativeArray<Entity> entities = new NativeArray<Entity>(chunkSize.x * chunkSize.y * chunkSize.z, Allocator.Temp);
        //    for (int x = 0; x < chunkSize.x; x++)
        //    {
        //        for (int y = 0; y < chunkSize.y; y++)
        //        {
        //            for (int z = 0; z < chunkSize.z; z++)
        //            {
        //                int index = x + (y + z * chunkSize.y) * chunkSize.x;
        //                entities[index] =
        //                beginCommandBuffer.CreateEntity(entityInQueryIndex, blockArchetype);
        //                beginCommandBuffer.SetComponent(entityInQueryIndex, entities[index], new BlockParentChunkTempData { Value = entity });
        //            }
        //        }
        //    }

        //    // TODO: Remove this loop and add to loop above
        //    for (int x = 0; x < chunkSize.x; x++)
        //    {
        //        for (int y = 0; y < chunkSize.y; y++)
        //        {
        //            for (int z = 0; z < chunkSize.z; z++)
        //            {
        //                BlockNeighboursData blockNeighboursData = new BlockNeighboursData();
        //                for (int i = 0; i < 6; i++)
        //                {
        //                    int3 neighbourPos = new int3(x, y, z) + neighbours[i];
        //                    if (!(neighbourPos.x < 0 || neighbourPos.x > chunkSize.x - 1 ||
        //                            neighbourPos.y < 0 || neighbourPos.y > chunkSize.y - 1 ||
        //                            neighbourPos.z < 0 || neighbourPos.z > chunkSize.z - 1))
        //                        blockNeighboursData[i] = entities[neighbourPos.x + (neighbourPos.y + neighbourPos.z * chunkSize.y) * chunkSize.x];
        //                }
        //                beginCommandBuffer.SetComponent<BlockNeighboursData>(entityInQueryIndex, 
        //                    entities[x + (y + z * chunkSize.y) * chunkSize.x], 
        //                    blockNeighboursData);
        //            }
        //        }
        //    }

        //    entities.Dispose();

        //    beginCommandBuffer.RemoveComponent<ChunkRequirePopulateTag>(entityInQueryIndex, entity);
        //})
        //    .WithNativeDisableParallelForRestriction(neighbours)
        //    .WithDisposeOnCompletion(neighbours)
        //    .ScheduleParallel(Dependency);

        //BeginEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        //jobHandle = Entities
        //    .WithoutBurst()
        //    .WithAll<BlockParentChunkTempData>()
        //    .ForEach((in BlockParentChunkTempData parentChunkTempData, in Entity entity, in int entityInQueryIndex) =>
        //{
        //    beginCommandBuffer.AddSharedComponent(entityInQueryIndex, entity, new BlockParentChunkData { Value = entity });
        //    beginCommandBuffer.RemoveComponent<BlockParentChunkTempData>(entityInQueryIndex, entity);
        //}).ScheduleParallel(jobHandle);

        //BeginEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
        //EndEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        //Dependency = jobHandle;
    }
}
