using Unity.Entities;

[InternalBufferCapacity(6)]
public struct BlockVisibleSideBufferElement : IBufferElementData
{
    // Back Front Top Bottom Left Right
    public bool Value;
}