using Unity.Entities;

[GenerateAuthoringComponent]
public struct ChunkEntitiesBufferData : IBufferElementData
{
    public Entity Value;
}
