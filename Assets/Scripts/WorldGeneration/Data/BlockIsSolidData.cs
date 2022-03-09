using Unity.Entities;

[GenerateAuthoringComponent]
public struct BlockIsSolidData : IComponentData
{
    public bool Value;
}