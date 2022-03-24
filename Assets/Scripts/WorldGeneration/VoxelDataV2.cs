using Unity.Mathematics;

public static class VoxelDataV2
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
}
