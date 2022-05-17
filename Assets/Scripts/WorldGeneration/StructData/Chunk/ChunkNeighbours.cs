
using Unity.Mathematics;

public class ChunkNeighbours
{
    public Chunk Back;
    public Chunk Front;

    public Chunk Top;
    public Chunk Bottom;

    public Chunk Left;
    public Chunk Right;

    public Chunk this[int i]
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

    public Chunk this[int3 pos]
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
}
