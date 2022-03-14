using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class ChunkInitializationSystem : SystemBase
{
    private BeginInitializationEntityCommandBufferSystem BeginEntityCommandBufferSystem;
    private EndInitializationEntityCommandBufferSystem EndEntityCommandBufferSystem;
    private int3 ChunkSize;// = new int3(4, 4, 4);

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

    protected override void OnCreate()
    {
        ChunkSize = new int3(ChunkCore.Size.x, ChunkCore.Size.y, ChunkCore.Size.z);
        BeginEntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        EndEntityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();

        translations = new NativeArray<Translation>(ChunkSize.x * ChunkSize.y * ChunkSize.z, Allocator.Persistent);

        for (int x = 0; x < ChunkSize.x; x++)
        {
            for (int y = 0; y < ChunkSize.y; y++)
            {
                for (int z = 0; z < ChunkSize.z; z++)
                {
                    translations[x + (y + z * ChunkSize.y) * ChunkSize.x] = new Translation() { Value = new float3(x, y, z) };
                }
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        translations.Dispose();
    }

    private struct CreateLingingListJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int3> neighbours;
        [ReadOnly] public int3 chunkSize;

        [ReadOnly] public NativeArray<Entity> blocks;
        public NativeArray<BlockNeighboursData> blockNeighbours;
        public void Execute(int index)
        {
            BlockNeighboursData neighboursData = new BlockNeighboursData();
            for (int i = 0; i < 6; i++)
            {
                int idx = index + neighbours[i].x + (neighbours[i].y + neighbours[i].z * chunkSize.y) * chunkSize.x;
                if (idx < 0 || idx >= blocks.Length)
                    continue;
                neighboursData[i] = blocks[idx];
            }
            blockNeighbours[index] = neighboursData;
        }
    }

    private struct LinkBlocksJob : IJob
    {
        [ReadOnly] public NativeArray<int3> neighbours;
        [ReadOnly] public NativeArray<Entity> blocks;
        [ReadOnly] public int3 chunkSize;
        public EntityCommandBuffer.ParallelWriter commandBuffer;
        public void Execute()
        {
            for (int x = 0; x < chunkSize.x; x++)
            {
                for (int y = 0; y < chunkSize.y; y++)
                {
                    for (int z = 0; z < chunkSize.z; z++)
                    {
                        BlockNeighboursData blockNeighboursData = new BlockNeighboursData();
                        for (int i = 0; i < 6; i++)
                        {
                            int3 neighbourPos = new int3(x, y, z) + neighbours[i];
                            if (!(neighbourPos.x < 0 || neighbourPos.x >= chunkSize.x ||
                                    neighbourPos.y < 0 || neighbourPos.y >= chunkSize.y ||
                                    neighbourPos.z < 0 || neighbourPos.z >= chunkSize.z))
                                blockNeighboursData[i] = blocks[neighbourPos.x + (neighbourPos.y + neighbourPos.z * chunkSize.y) * chunkSize.x];
                        }
                        commandBuffer.SetComponent(0, blocks[x + (y + z * chunkSize.y) * chunkSize.x], blockNeighboursData);
                        //commandBuffer.SetComponent(0, blocks[x + (y + z * chunkSize.y) * chunkSize.x], new Translation { Value = new float3(x, y, z) });
                    }
                }
            }
        }
    }

    protected override void OnUpdate()
    {
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
            typeof(BlockGenerateDataTag),
            typeof(BlockRequireUpdateTag)
            );

        EntityQuery chunksRequirePopulateQuerry = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ChunkRequirePopulateTag>());
        NativeArray<Entity> chunksRequirePopulate = chunksRequirePopulateQuerry.ToEntityArray(Allocator.Temp);

        foreach (Entity chunk in chunksRequirePopulate)
        {
            NativeArray<Entity> blocks = EntityManager.CreateEntity(blockArchetype, chunkBitSize, Allocator.TempJob);
            EntityManager.AddComponent(blocks, ComponentType.ReadOnly<BlockRequirePopulateTag>());

            for (int i = 0; i < blocks.Length; i++)
            {
                EntityManager.SetComponentData(blocks[i], new BlockIdData { Value = i });
            }

            EntityQuery entityQuery = EntityManager.CreateEntityQuery(new ComponentType[] { ComponentType.ReadOnly<BlockRequirePopulateTag>() });
            EntityManager.AddSharedComponentData(entityQuery, new BlockParentChunkData { Value = chunk });

            NativeArray<BlockNeighboursData> blockNeighboursDatas = new NativeArray<BlockNeighboursData>(chunkBitSize, Allocator.TempJob);

            CreateLingingListJob job = new CreateLingingListJob
            {
                blocks = blocks,
                chunkSize = chunkSize,
                neighbours = neighbours,
                blockNeighbours = blockNeighboursDatas
            };

            /*
            LinkBlocksJob job = new LinkBlocksJob
            {
                blocks = blocks,
                chunkSize = chunkSize,
                commandBuffer = beginCommandBuffer,
                neighbours = neighbours
            };
            */
            JobHandle jobHandle = job.Schedule(chunkBitSize, 64);

            //BeginEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();

            EntityManager.AddComponentData(entityQuery, translations);
            EntityManager.AddComponentData(entityQuery, blockNeighboursDatas);
            EntityManager.RemoveComponent<BlockRequirePopulateTag>(entityQuery);
            blockNeighboursDatas.Dispose(jobHandle);
            blocks.Dispose();
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
