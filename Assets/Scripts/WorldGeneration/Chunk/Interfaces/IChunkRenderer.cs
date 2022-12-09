using UnityEngine;

public interface IChunkRenderer
{
    public void Render(ChunkNeighbours neighbours);
    public bool RequireProcessing();
    public void Destroy();
    public void Update();
    // public void UpdateData();
    public ComputeBuffer GetBlocksBuffer();
    // public void Unload();
}