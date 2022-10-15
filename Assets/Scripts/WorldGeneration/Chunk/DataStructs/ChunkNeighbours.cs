
using Unity.Mathematics;

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

    public IChunk this[int3 pos]
    {
        get
        {
            for (int i = 0; i < 6; i++)
            {
                if (math.all(pos == VoxelData.voxelNeighbours[i])) return this[i];
            }

            throw new System.Exception("Index out of bounds");
        }
    }

    public bool IsValid()
    {
        for (int i = 0; i < 6; i++)
        {
            if (this[i] == null || this[i].IsDestroyed()) return false;
        }
        return true;
    }

    public bool GetData(out ChunkNeighbourData neighbourData)
    {
        // neighbourData = new ChunkNeighbourData();
        // if (!IsValid()) return false;
        // for (int i = 0; i < 6; i++)
        // {
        //     if (!this[i].CanAccess()) return false;
        //     neighbourData.ChunkNeighbourDataArray[i] = this[i].GetSharedData();
        // }
        // return true;

        neighbourData = new ChunkNeighbourData();
        // if (!IsValid()) return false;
        for (int i = 0; i < 6; i++)
        {
            if (this[i] != null && !this[i].IsDestroyed() && this[i].CanAccess())
                neighbourData.ChunkNeighbourDataArray[i] = this[i].GetSharedData();
            else
                neighbourData.ChunkNeighbourDataArray[i] = ChunkRendererConst.voidChunkBlockId;
        }
        return true;
    }

    public void ReleaseData()
    {
        for (int i = 0; i < 6; i++)
        {
            if (this[i] != null)
                this[i].ReleaseSharedData();
        }
    }
}
