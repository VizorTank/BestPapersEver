public interface IWorld
{
    public void DrawChunks();
    public void CreateNewChunk();
    public void SetChunkActive();
    public void GetBiome();
    public void TryGetBlock();
    public void TrySetBlock();
    public void TryPlaceBlock();
    public void TryGetGlocks();
    public void GetNeighbours();
    public void IsInWorld();
}