using Unity.Collections;

public struct ChunkNeighbourData
{
    // Back Front Top Bottom Left Right
    public NativeArray<int> Back;
    public NativeArray<int> Front;

    public NativeArray<int> Top;
    public NativeArray<int> Bottom;

    public NativeArray<int> Left;
    public NativeArray<int> Right;

    public NativeArray<int> this[int i]
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
                5 => Right
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
}
