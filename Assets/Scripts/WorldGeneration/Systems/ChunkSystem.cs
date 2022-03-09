using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Rendering;
using System.Threading;
using Unity.Collections;
using System.Collections.Generic;

public class ChunkSystem : SystemBase
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        JobHandle firstHandle = GenerateVoxels(Dependency);
        JobHandle secondHandle = CheckVisibilityV2(firstHandle);
        //JobHandle thirdHandle = GenerateMesh(secondHandle);

        Dependency = secondHandle;// JobHandle.CombineDependencies(firstHandle, secondHandle);
    }

    public JobHandle GenerateVoxels(JobHandle depedency)
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        int3 chunkSize = new int3(ChunkEntities.Size.x, ChunkEntities.Size.y, ChunkEntities.Size.z);
        float scale = 0.5f;
        float offset = 0;
        int terrainHeightDifference = 16;
        int terrainSolidGround = 16;

        JobHandle jobHandle = Entities.WithAll<BlockGenerateDataTag>().ForEach((ref BlockIdData blockIdData, ref BlockIsSolidData blockIsSolid, in Entity entity, in int entityInQueryIndex, in Translation translation) =>
        {
            int yPos = (int)math.floor(translation.Value.y);

            byte voxelValue = 0;

            int terrainHeight = (int)math.floor((noise.snoise(
                new float2(
                    translation.Value.x / chunkSize.x * scale + offset,
                    translation.Value.z / chunkSize.z * scale + offset)) + 1.0) / 2 * terrainHeightDifference) + terrainSolidGround;

            if (yPos > terrainHeight)
                voxelValue = 0;
            else if (yPos == terrainHeight)
                voxelValue = 3;
            else if (yPos > terrainHeight - 4)
                voxelValue = 4;
            else if (yPos < terrainHeight)
                voxelValue = 2;

            blockIdData.Value = voxelValue;
            if (blockIdData.Value == 0)
                blockIsSolid.Value = false;
            else
                blockIsSolid.Value = true;

            commandBuffer.RemoveComponent<BlockGenerateDataTag>(entityInQueryIndex, entity);
        }).ScheduleParallel(depedency);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        return jobHandle;
    }

    public JobHandle CheckVisibility(JobHandle depedency)
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        //NativeHashMap<Entity, bool> chunkHashMap = new NativeHashMap<Entity, bool>(16, Allocator.TempJob);

        //bool iii = false;

        JobHandle jobHandle = Entities.WithAll<BlockIsVisibleTag, BlockRequireUpdateTag>().ForEach((ref BlockVisibleSidesData blockVisibleSides, in BlockNeighboursData blockNeighbours, in int entityInQueryIndex, in Entity entity) =>
        {
            //Monitor.TryEnter(iii);
            //chunkHashMap.TryAdd(blockParentChunk.Value, true);

            blockVisibleSides.Back = blockNeighbours.Back == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Back).Value;
            blockVisibleSides.Front = blockNeighbours.Front == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Front).Value;

            blockVisibleSides.Top = blockNeighbours.Top == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Top).Value;
            blockVisibleSides.Bottom = blockNeighbours.Bottom == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Bottom).Value;

            blockVisibleSides.Left = blockNeighbours.Left == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Left).Value;
            blockVisibleSides.Right = blockNeighbours.Right == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Right).Value;

            if (!blockVisibleSides.Back && !blockVisibleSides.Front &&
                !blockVisibleSides.Top && !blockVisibleSides.Bottom &&
                !blockVisibleSides.Left && !blockVisibleSides.Right)
            {
                commandBuffer.RemoveComponent<BlockIsVisibleTag>(entityInQueryIndex, entity);
            }
            commandBuffer.RemoveComponent<BlockRequireUpdateTag>(entityInQueryIndex, entity);
        }).ScheduleParallel(depedency);

        //Monitor.Exit(iii);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        return jobHandle;
    }

    public JobHandle CheckVisibilityV2(JobHandle depedency)
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        List<BlockParentChunkData> chunks = new List<BlockParentChunkData>();
        JobHandle lastJobHandle = depedency;

        EntityManager.GetAllUniqueSharedComponentData(chunks);

        NativeHashMap<Entity, bool> chunkHashMap = new NativeHashMap<Entity, bool>(16, Allocator.TempJob);
        var chunkHashMapWriter = chunkHashMap.AsParallelWriter();

        foreach (BlockParentChunkData chunk in chunks)
        {
            lastJobHandle = Entities.WithSharedComponentFilter(chunk).WithAll<BlockIsVisibleTag, BlockRequireUpdateTag>().ForEach((ref BlockVisibleSidesData blockVisibleSides, in BlockNeighboursData blockNeighbours, in int entityInQueryIndex, in Entity entity) =>
            {
                //Monitor.TryEnter(iii);
                chunkHashMapWriter.TryAdd(chunk.Value, true);

                blockVisibleSides.Back = blockNeighbours.Back == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Back).Value;
                blockVisibleSides.Front = blockNeighbours.Front == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Front).Value;

                blockVisibleSides.Top = blockNeighbours.Top == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Top).Value;
                blockVisibleSides.Bottom = blockNeighbours.Bottom == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Bottom).Value;

                blockVisibleSides.Left = blockNeighbours.Left == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Left).Value;
                blockVisibleSides.Right = blockNeighbours.Right == Entity.Null || !GetComponent<BlockIsSolidData>(blockNeighbours.Right).Value;

                if (!blockVisibleSides.Back && !blockVisibleSides.Front &&
                    !blockVisibleSides.Top && !blockVisibleSides.Bottom &&
                    !blockVisibleSides.Left && !blockVisibleSides.Right)
                {
                    commandBuffer.RemoveComponent<BlockIsVisibleTag>(entityInQueryIndex, entity);
                }
                commandBuffer.RemoveComponent<BlockRequireUpdateTag>(entityInQueryIndex, entity);
            }).ScheduleParallel(lastJobHandle);
        }

        m_EntityCommandBufferSystem.AddJobHandleForProducer(lastJobHandle);

        //Monitor.Exit(iii);
        chunkHashMap.Dispose(lastJobHandle);

        return lastJobHandle;
    }
}
