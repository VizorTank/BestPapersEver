using Unity.Collections;
using Unity.Mathematics;

public struct ChunkGeneraionBiomes
{
    public int3 ChunkSize;
    public int3 ChunkCoords;

    public int Center;
    [DeallocateOnJobCompletion]
    public NativeArray<int> NeighbourBiomeIds;
    public NativeArray<int3> BiomeNeighbours;
    public NativeArray<BiomeAttributesStruct> Biomes;
    public NativeArray<LodeStruct> Lodes;

    public int WaterLevel;

    public void Destroy()
    {
        try { NeighbourBiomeIds.Dispose(); } catch { }
    }

    public int CalculateTerrainHeight(int3 position)
    {
        int3 localPosition = (((position % ChunkSize) + ChunkSize) % ChunkSize);
        localPosition.y = 0;
        // float3 quaterPosition = math.sign(new float3(localPosition - ChunkSize / 2));
        // quaterPosition.y = 1;

        float distance = GetDistance(new int3(), localPosition);
        float totalDistance = distance;
        float terrainHeight = Biomes[Center].CalculateTerrainHeight(position) * distance;

        for (int i = 0; i < 8; i++)
        {
            // if (math.any(quaterPosition + BiomeNeighbours[i] == 0)) continue;
            distance = GetDistance(BiomeNeighbours[i], localPosition);
            terrainHeight += Biomes[NeighbourBiomeIds[i]].CalculateTerrainHeight(position) * distance;
            totalDistance += distance;
        }

        if (totalDistance != 0)
            terrainHeight /= totalDistance;
        return (int)math.round(terrainHeight);
    }

    private float GetDistance(int3 center, int3 pos)
    {
        int3 cChunkSize = ChunkSize - (ChunkSize + 1) % 2;
        float3 localPos = pos - (center + new float3(0.5)) * cChunkSize;

        return math.clamp(1 - GetLength(localPos / (new float3(cChunkSize) * 1.5f)), 0, 1);
    }

    private float GetLength(float3 vector)
    {
        // int3 cChunkSize = ChunkSize - (ChunkSize + 1) % 2;
        // vector /= (new float3(cChunkSize) * 1.5f);

        // return math.length(vector);
        return math.max(math.abs(vector.x), math.abs(vector.z));
    }

    public int GetBlock(float3 position)
    {
        return Biomes[Center].GetBlock(position, CalculateTerrainHeight(new int3(position)), Lodes, WaterLevel);
    }
}