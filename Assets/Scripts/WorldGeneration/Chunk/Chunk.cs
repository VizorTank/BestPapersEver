using Unity.Collections;
using Unity.Mathematics;

public class Chunk
{
    private int3 coordinates;
    public int3 Coordinates => coordinates;
    private static int3 Size { get => VoxelData.ChunkSize; }
    public float3 ChunkPosition => new float3(coordinates.x, coordinates.y, coordinates.z) * Size;

    private WorldClass world;
    private ChunkGenerator chunkGenerator;
    public ChunkGenerator ChunkGenerator => chunkGenerator;


    public Chunk(int3 _position, WorldClass _world, BiomeAttributesStruct _biome)
    {
        coordinates = _position;
        world = _world;

        chunkGenerator = new ChunkGenerator(this, world, _biome);
    }
    public void Destroy() => chunkGenerator.Destroy();
    public void SetNeighbours(ChunkNeighbours chunkNeighbours) => chunkGenerator.SetNeighbours(chunkNeighbours);
    public bool CanEditChunk() => chunkGenerator.CanEditChunk();
    public void NeighbourDepecency(int value) => chunkGenerator.NeighbourDepecency(value);
    public int GetIndex(int3 position) => position.x + (position.y + position.z * Size.y) * Size.x;
    public int GetBlock(int3 position) => chunkGenerator.GetBlock(position);
    public int SetBlock(int3 position, int value) => chunkGenerator.SetBlock(position, value);
    public NativeArray<int> GetBlocks() => chunkGenerator.GetBlocks();

    public bool TryPlaceBlock(int3 position, int blockID)
    {
        if (!CanEditChunk()) return false;
        if (!world.blockTypesList.areReplacable[GetBlock(position)]) return false;

        SetBlock(position, blockID);
        return true;
    }
}
