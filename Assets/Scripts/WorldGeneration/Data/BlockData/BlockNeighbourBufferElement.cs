using Unity.Entities;

[InternalBufferCapacity(6)]
public struct BlockNeighbourBufferElement : IBufferElementData
{
    // Back Front Top Bottom Left Right
    public Entity Value;
}
