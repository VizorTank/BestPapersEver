public interface IChunkRenderer
{
    public void Render(ChunkNeighbours neighbours);
    public void Render();
    public bool RequireProcessing();
    public void Destroy();
    public bool CanAccess();
    public void Update();
    public void Unload();
}