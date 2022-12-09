
using Unity.Mathematics;
using UnityEngine;

public class ChunkNeighbours
{
    public IChunk Back;
    public IChunk Front;

    public IChunk Top;
    public IChunk Bottom;

    public IChunk Left;
    public IChunk Right;

    public IChunk this[int i]
    {
        get
        {
            return i switch
            {
                0 => Back,
                1 => Front,
                2 => Top,
                3 => Bottom,
                4 => Left,
                5 => Right,
                _ => throw new System.Exception("Index out of bounds")
            };
        }
        set
        {
            switch (i)
            {
                case 0: Back = value; break;
                case 1: Front = value; break;
                case 2: Top = value; break;
                case 3: Bottom = value; break;
                case 4: Left = value; break;
                case 5: Right = value; break;
            }
        }
    }

    public ChunkNeighbourData GetData()
    {
        ChunkNeighbourData neighbourData = new ChunkNeighbourData();
        for (int i = 0; i < 6; i++)
        {
            if (this[i] != null && !this[i].IsDestroyed() && this[i].CanAccess())
            {
                neighbourData.ChunkNeighbourDataArray[i] = this[i].GetSharedData();
            }
            else
            {
                neighbourData.ChunkNeighbourDataArray[i] = ChunkRendererConst.voidChunkBlockId;
                // Debug.Log("This was not suposed to be used!");
            }
        }
        return neighbourData;
    }

    public ChunkNeighbourDataBuffers GetBufferData()
    {
        ChunkNeighbourDataBuffers buffers = new ChunkNeighbourDataBuffers();

        for (int i = 0; i < 6; i++)
        {
            if (this[i] != null && !this[i].IsDestroyed() && this[i].CanAccess())
                buffers[i] = this[i].GetBlocksBuffer();
            else
                buffers[i] = ChunkRendererConst.voidChunkBlockIdBuffer;
        }

        return buffers;
    }

    public void ReleaseData()
    {
        for (int i = 0; i < 6; i++)
        {
            if (this[i] != null && this[i].CanAccess())
                this[i].ReleaseSharedData();
        }
    }

    public void UpdateNeighbours()
    {
        for (int i = 0; i < 6; i++)
        {
            if (this[i] != null && !this[i].IsDestroyed() && this[i].CanAccess())
            {
                this[i].Update();
            }
        }
    }

    public void FillMissingNeighbours(IWorld world, IChunk chunk)
    {
        for (int i = 0; i < 6; i++)
        {
            if (this[i] == null || this[i].IsDestroyed())
            {
                this[i] = world.GetChunk(chunk.GetChunkCoordinates() + VoxelData.voxelNeighbours[i]);
            }
        }
    }

    public void UpdateNeighboursNeighbourList()
    {
        for (int i = 0; i < 6; i++)
        {
            if (this[i] != null && !this[i].IsDestroyed() && this[i].CanAccess())
            {
                this[i].UpdateListOfNeighbours();
                this[i].Update();
            }
        }
    }
}
