using Unity.Entities;

[GenerateAuthoringComponent]
public struct BlockNeighboursData : IComponentData
{
    // Back Front Top Bottom Left Right
    public Entity Back;
    public Entity Front;

    public Entity Top;
    public Entity Bottom;

    public Entity Left;
    public Entity Right;

    public Entity this[int i]
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
                _ => Entity.Null,
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
