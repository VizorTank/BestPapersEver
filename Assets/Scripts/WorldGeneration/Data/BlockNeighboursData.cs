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
}
