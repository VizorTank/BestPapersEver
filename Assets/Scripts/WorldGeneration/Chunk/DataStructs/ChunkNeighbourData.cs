using Unity.Collections;

public struct ChunkNeighbourData
{
    // Back Front Top Bottom Left Right
    public ChunkNeighbourDataArray ChunkNeighbourDataArray;
    public ChunkNeighbourDataValid ChunkNeighbourDataValid;

    // public NativeArray<int> this[int i]
    // {
    //     get => ChunkNeighbourDataArray[i];
    //     set => ChunkNeighbourDataArray[i] = value;
    // }
    public void Destroy()
    {
        for (int i = 0; i < 6; i++)
        {
            if (!ChunkNeighbourDataValid[i])
                try { ChunkNeighbourDataArray[i].Dispose(); } catch { }
        }
    }
}

public struct ChunkNeighbourDataArray
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
}

public struct ChunkNeighbourDataValid
{
    // Back Front Top Bottom Left Right
    public bool Back;
    public bool Front;
           
    public bool Top;
    public bool Bottom;
           
    public bool Left;
    public bool Right;

    public bool this[int i]
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
}
