using Unity.Entities;

public struct ChunkData : ISharedComponentData
{
    public int xSize;
    public int ySize;
    public int zSize;
}
