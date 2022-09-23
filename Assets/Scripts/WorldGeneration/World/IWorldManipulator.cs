using Unity.Mathematics;

public interface IWorldManipulator
{
    public bool TryGetChunk(int3 chunkCoordinates, ref IChunk chunk);
    public IChunk GetOrCreateChunk(int3 chunkCoordinates);
}