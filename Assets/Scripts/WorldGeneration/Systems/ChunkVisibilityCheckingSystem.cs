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

    //private struct CheckVisibilityJob : IJobFor
    //{
    //    Entity chunk;

    //    NativeArray<Entity> entities;
    //    NativeArray<BlockNeighboursData> blockNeighbours;
    //    NativeArray<BlockVisibleSidesData> blockVisibleSides;

    //    NativeHashMap<Entity, bool>.ParallelWriter chunkHashMapWriter;

    //    int removeVisibleTagIndex;
    //    NativeArray<Entity> removeVisibleTag;
    //    int addVisibleTagIndex;
    //    NativeArray<Entity> addVisibleTag;
    //    public void Execute(int index)
    //    {
    //        BlockVisibleSidesData blockVisibleSidesData = new BlockVisibleSidesData();
    //        for (int i = 0; i < 6; i++)
    //            blockVisibleSidesData[i] = blockNeighbours[index][i] == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours[index][i]).Value;

    //        blockVisibleSides[index] = blockVisibleSidesData;

    //        if (!(blockVisibleSidesData.Back || blockVisibleSidesData.Front ||
    //                blockVisibleSidesData.Top || blockVisibleSidesData.Bottom ||
    //                blockVisibleSidesData.Left || blockVisibleSidesData.Right))
    //            //commandBuffer.RemoveComponent<BlockIsVisibleTag>(entityInQueryIndex, entity);
    //            removeVisibleTag[removeVisibleTagIndex++] = entities[index];
    //        else
    //            addVisibleTag[addVisibleTagIndex++] = entities[index];

    //        //if (chunkHashMapWriter.TryAdd(chunk.Value, true))
    //        //    commandBuffer.AddComponent<ChunkRequireUpdateTag>(entityInQueryIndex, chunk.Value);

    //        //commandBuffer.RemoveComponent<BlockRequireUpdateTag>(entityInQueryIndex, entity);
    //    }
    //}

    NativeList<Entity> totalRemoveVisibleTag;
    NativeList<Entity>.ParallelWriter totalRemoveVisibleTagWriter;
    NativeList<Entity> totalAddVisibleTag;
    NativeList<Entity>.ParallelWriter totalAddVisibleTagWriter;
    NativeList<Entity> totalRemoveUpdateTag;
    NativeList<Entity>.ParallelWriter totalRemoveUpdateTagWriter;

    protected override void OnUpdate()
    {
        var commandBuffer = EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        List<BlockParentChunkData> chunks = new List<BlockParentChunkData>();
        EntityManager.GetAllUniqueSharedComponentData(chunks);
        NativeHashMap<Entity, bool> chunkHashMap = new NativeHashMap<Entity, bool>(chunks.Count + 1, Allocator.TempJob);

        EntityQueryDesc entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<BlockRequireUpdateTag>(),
                ComponentType.ReadOnly<BlockNeighboursData>(),
                ComponentType.ReadOnly<BlockParentChunkData>(),
                ComponentType.ReadOnly<BlockVisibleSidesData>()
            }
        };

        EntityQuery entityQuery = EntityManager.CreateEntityQuery(entityQueryDesc);

        NativeHashMap<Entity, bool>.ParallelWriter chunkHashMapWriter = chunkHashMap.AsParallelWriter();

        totalRemoveVisibleTag = new NativeList<Entity>(0, Allocator.TempJob);
        totalRemoveVisibleTagWriter = totalRemoveVisibleTag.AsParallelWriter();
        totalAddVisibleTag = new NativeList<Entity>(0, Allocator.TempJob);
        totalAddVisibleTagWriter = totalAddVisibleTag.AsParallelWriter();
        totalRemoveUpdateTag = new NativeList<Entity>(0, Allocator.TempJob);
        totalRemoveUpdateTagWriter = totalRemoveUpdateTag.AsParallelWriter();

        JobHandle lastJobHandle = Dependency;
        int i = 0;
        NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>(chunks.Count, Allocator.Temp);
        foreach (BlockParentChunkData chunk in chunks)
        {
            entityQuery.SetSharedComponentFilter(chunk);
            NativeList<Entity> removeVisibleTag = new NativeList<Entity>(entityQuery.CalculateEntityCount(), Allocator.TempJob);
            var removeVisibleTagWriter = removeVisibleTag.AsParallelWriter();
            NativeList<Entity> addVisibleTag = new NativeList<Entity>(entityQuery.CalculateEntityCount(), Allocator.TempJob);
            var addVisibleTagWriter = addVisibleTag.AsParallelWriter();
            NativeList<Entity> removeUpdateTag = new NativeList<Entity>(entityQuery.CalculateEntityCount(), Allocator.TempJob);
            var removeUpdateTagWriter = removeUpdateTag.AsParallelWriter();

            jobHandles[i++] = Entities
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
                {
                    //commandBuffer.RemoveComponent<BlockIsVisibleTag>(entityInQueryIndex, entity);
                    removeVisibleTagWriter.AddNoResize(entity);
                }
                else
                {
                    //commandBuffer.AddComponent<BlockIsVisibleTag>(entityInQueryIndex, entity);
                    addVisibleTagWriter.AddNoResize(entity);
                }

                //if (chunkHashMapWriter.TryAdd(chunk.Value, true))
                //    commandBuffer.AddComponent<ChunkRequireUpdateTag>(entityInQueryIndex, chunk.Value);
                chunkHashMapWriter.TryAdd(chunk.Value, true);
                //commandBuffer.RemoveComponent<BlockRequireUpdateTag>(entityInQueryIndex, entity);
                removeUpdateTagWriter.AddNoResize(entity);
            }).ScheduleParallel(Dependency);

            jobHandles[i - 1].Complete();

            totalRemoveVisibleTag.AddRange(removeVisibleTag);
            totalAddVisibleTag.AddRange(addVisibleTag);
            totalRemoveUpdateTag.AddRange(removeUpdateTag);

            removeVisibleTag.Dispose();
            addVisibleTag.Dispose();
            removeUpdateTag.Dispose();
        }
        //EntityCommandBufferSystem.AddJobHandleForProducer(lastJobHandle);

        JobHandle combineJobHandels = JobHandle.CombineDependencies(jobHandles);


        combineJobHandels.Complete();
        SetComponents();

        var t = chunkHashMap.GetKeyArray(Allocator.Temp);
        EntityManager.AddComponent<ChunkRequireUpdateTag>(t);
        t.Dispose();

        chunkHashMap.Dispose(lastJobHandle);

        Dependency = lastJobHandle;
    }

    void SetComponents()
    {
        var tmp = totalRemoveVisibleTag.ToArray(Allocator.Temp);
        EntityManager.RemoveComponent<BlockIsVisibleTag>(tmp);
        tmp.Dispose();
        tmp = totalAddVisibleTag.ToArray(Allocator.Temp);
        EntityManager.AddComponent<BlockIsVisibleTag>(tmp);
        tmp.Dispose();
        tmp = totalRemoveUpdateTag.ToArray(Allocator.Temp);
        EntityManager.RemoveComponent<BlockRequireUpdateTag>(tmp);
        tmp.Dispose();

        totalRemoveVisibleTag.Dispose();
        totalAddVisibleTag.Dispose();
        totalRemoveUpdateTag.Dispose();
    }
}
