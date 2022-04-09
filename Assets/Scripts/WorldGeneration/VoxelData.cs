using Unity.Mathematics;
using UnityEngine.Rendering;

public static class VoxelData
{
    public static readonly float3[] voxelVerts = new float3[]
    {
        new float3(0.0f, 0.0f, 0.0f),
        new float3(1.0f, 0.0f, 0.0f),
        new float3(1.0f, 1.0f, 0.0f),
        new float3(0.0f, 1.0f, 0.0f),
        new float3(0.0f, 0.0f, 1.0f),
        new float3(1.0f, 0.0f, 1.0f),
        new float3(1.0f, 1.0f, 1.0f),
        new float3(0.0f, 1.0f, 1.0f)
    };

    public static readonly int3[] voxelNeighbours = new int3[]
    {
        new int3(0, 0, -1),
        new int3(0, 0, 1),
        new int3(0, 1, 0),
        new int3(0, -1, 0),
        new int3(-1, 0, 0),
        new int3(1, 0, 0),
    };

    public static readonly int voxelTrisSize = 4;

    public static readonly int[] voxelTris =
    {
        0, 3, 1, 2,
        5, 6, 4, 7,
        3, 7, 2, 6,
        1, 5, 0, 4,
        4, 7, 0, 3,
        1, 2, 5, 6
    };

    public static readonly float2[] voxelUvs = new float2[]
    {
        new float2(0.0f, 0.0f),
        new float2(0.0f, 1.0f),
        new float2(1.0f, 0.0f),
        new float2(1.0f, 1.0f)
    };

    public static VertexAttributeDescriptor[] layoutVertex = new VertexAttributeDescriptor[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
    };

    public static int[] triangleOrder = new[] { 0, 1, 2, 2, 1, 3 };

    public static int3[] axisArray = new[]
    {
        new int3(0, 0, 1),
        new int3(0, 1, 0),
        new int3(1, 0, 0)
    };
}
