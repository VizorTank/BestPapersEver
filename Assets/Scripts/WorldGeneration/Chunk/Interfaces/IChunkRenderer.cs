public interface IChunkRenderer
{
    public void Render(ChunkNeighbours neighbours);
    public bool RequireProcessing();
}