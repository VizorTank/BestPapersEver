using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct BlockVisibleSidesData : IComponentData
{
    // Back Front Top Bottom Left Right
    public bool Back;
    public bool Front;
    
    public bool Top;
    public bool Bottom;

    public bool Left;
    public bool Right;
}
