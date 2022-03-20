using Unity.Entities;

[InternalBufferCapacity(0)]
public struct ChunkMeshBufferElement : IBufferElementData
{
    // Back Front Top Bottom Left Right
    public Entity Value;
}
