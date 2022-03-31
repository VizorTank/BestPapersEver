using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

//[DisableAutoCreation]
public class ChunkGeneratingWorldSystem : SystemBase
{
    private EndInitializationEntityCommandBufferSystem EntityCommandBufferSystem;
    private int3 ChunkSize = ChunkV4.Size;

    protected override void OnCreate()
    {
        EntityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        int3 chunkSize = ChunkSize;
        float scale = 0.2f;
        float offset = 0;
        int terrainHeightDifference = 4;
        int terrainSolidGround = 12;

        JobHandle jobHandle = Entities
            .WithAll<BlockGenerateDataTag>()
            .ForEach(
            (ref BlockIdData blockIdData, ref BlockIsSolidData blockIsSolid, 
            in Entity entity, in int entityInQueryIndex, in Translation translation) =>
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
            commandBuffer.AddComponent<BlockRequireUpdateTag>(entityInQueryIndex, entity);
        }).ScheduleParallel(Dependency);

        EntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        Dependency = jobHandle;
    }
}
