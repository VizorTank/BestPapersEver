public struct ClusterSidesVisibility
{
    // Back Front Top Bottom Left Right
    public int Back;
    public int Front;

    public int Top;
    public int Bottom;

    public int Left;
    public int Right;

    public int this[int i]
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
                _ => -1,
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
