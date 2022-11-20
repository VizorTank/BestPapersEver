public interface IChunkRenderer
{
    public void Render(ChunkNeighbours neighbours);
    public bool RequireProcessing();
    public void Destroy();
    public void Update();
    public void UpdateData();
    public void Unload();
}