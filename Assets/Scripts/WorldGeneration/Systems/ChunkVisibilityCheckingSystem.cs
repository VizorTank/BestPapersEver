using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public class ChunkVisibilityCheckingSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var commandBuffer = EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        List<BlockParentChunkData> chunks = new List<BlockParentChunkData>();
        EntityManager.GetAllUniqueSharedComponentData(chunks);
        NativeHashMap<Entity, bool> chunkHashMap = new NativeHashMap<Entity, bool>(chunks.Count, Allocator.TempJob);
        var chunkHashMapWriter = chunkHashMap.AsParallelWriter();

        JobHandle lastJobHandle = Dependency;
        foreach (BlockParentChunkData chunk in chunks)
        {
            lastJobHandle = Entities
                .WithSharedComponentFilter(chunk)
                .WithAll<BlockRequireUpdateTag>()
                .ForEach(
                (ref BlockVisibleSidesData blockVisibleSides, 
                in BlockNeighboursData blockNeighbours, in int entityInQueryIndex, in Entity entity) =>
            {
                for (int i = 0; i < 6; i++)
                    blockVisibleSides[i] = blockNeighbours[i] == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours[i]).Value;

                if (!(blockVisibleSides.Back || blockVisibleSides.Front ||
                        blockVisibleSides.Top || blockVisibleSides.Bottom ||
                        blockVisibleSides.Left || blockVisibleSides.Right))
                    commandBuffer.RemoveComponent<BlockIsVisibleTag>(entityInQueryIndex, entity);
                else
                    commandBuffer.AddComponent<BlockIsVisibleTag>(entityInQueryIndex, entity);

                if (chunkHashMapWriter.TryAdd(chunk.Value, true))
                    commandBuffer.AddComponent<ChunkRequireUpdateTag>(entityInQueryIndex, chunk.Value);

                commandBuffer.RemoveComponent<BlockRequireUpdateTag>(entityInQueryIndex, entity);
            }).ScheduleParallel(lastJobHandle);
        }
        EntityCommandBufferSystem.AddJobHandleForProducer(lastJobHandle);

        chunkHashMap.Dispose(lastJobHandle);

        Dependency = lastJobHandle;
    }
}
