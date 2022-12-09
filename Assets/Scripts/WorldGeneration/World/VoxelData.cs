using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public static class VoxelData
{
    public static readonly int ChunkSideSize = 16;
    public static readonly int3 ChunkSize = new int3(ChunkSideSize, ChunkSideSize, ChunkSideSize);

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

    public static int3[] clusterSidesArray = new int3[]
    {
        new int3(1, 1, 0),
        new int3(1, 1, 0),
        new int3(1, 0, 1),
        new int3(1, 0, 1),
        new int3(0, 1, 1),
        new int3(0, 1, 1)
    };

    public static int GetVolume() => ChunkSize.x * ChunkSize.y * ChunkSize.z;
    public static int3 GetPosition(int index)
    {
        int3 result = new int3();

        result.x = index % ChunkSize.x;
        result.y = index / ChunkSize.x % ChunkSize.y;
        result.z = index / ChunkSize.x / ChunkSize.y;

        return result;
    }
    public static int GetIndex(int3 position) => position.x + (position.y + position.z * ChunkSize.y) * ChunkSize.x;
    public static int3 GetChunkCoordinates(float3 position)
    {
        int3 cPos = new int3(Mathf.FloorToInt(position.x / ChunkSize.x),
            Mathf.FloorToInt(position.y / ChunkSize.y),
            Mathf.FloorToInt(position.z / ChunkSize.z));
        return cPos;
    }

    public static int3[] BiomeNeighbours = new int3[]{
        new int3(1, 0, 0),
        new int3(1, 0, 1),
        new int3(0, 0, 1),
        new int3(-1, 0, 1),
        new int3(-1, 0, 0),
        new int3(-1, 0, -1),
        new int3(0, 0, -1),
        new int3(1, 0, -1)
    };
}
