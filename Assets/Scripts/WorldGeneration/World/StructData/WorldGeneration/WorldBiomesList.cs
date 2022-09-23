using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

[System.Serializable]
public class WorldBiomesList
{
    public List<BiomeAttributes> Biomes;
    public float BiomeDensity;
    public float OceanDensity;
    public int WaterLevel;

    // private NativeArray<BiomeAttributesStruct> _biomeStructs;
    private NativeArray<BiomeAttributesStruct> _biomeStructs;

    public NativeArray<BiomeAttributesStruct> GetBiomes()
    {
        if (_biomeStructs == null || _biomeStructs.Length != Biomes.Count)
        {
            Init();
        }
        return _biomeStructs;
    }

    public NativeArray<LodeStruct> GetBiomeLodes(int3 ChunkCoord)
    {
        return Biomes[GetBiomeIndex(ChunkCoord)].GetBiomeLodes();
    }

    public void Init()
    {
        if (Biomes.Count <= 0) throw new System.Exception("No biomes found");
        List<LodeStruct> lodes = new List<LodeStruct>();

        _biomeStructs = new NativeArray<BiomeAttributesStruct>(Biomes.Count, Allocator.Persistent);
        // _biomeStructs = new BiomeAttributesStruct[Biomes.Count];
        for (int i = 0; i < Biomes.Count; i++)
        {
            for (int j = 0; j < Biomes[i].lodes.Count; j++)
            {
                LodeStruct lodeStruct = Biomes[i].lodes[j].GetLodeStruct();
                if (!lodes.Contains(lodeStruct))
                    lodes.Add(lodeStruct);
            }
            _biomeStructs[i] = Biomes[i].GetBiomeStruct();
        }
    }

    

    public BiomeAttributesStruct GetBiomeStruct(int3 ChunkCoord)
    {
        return GetBiomeStruct(GetBiomeIndex(ChunkCoord));
    }
    public BiomeAttributesStruct GetBiomeStruct(int index)
    {
        if (_biomeStructs == null || _biomeStructs.Length != Biomes.Count) Init();
        return _biomeStructs[index];
    }

    public int GetBiomeIndex(int3 ChunkCoord)
    {
        // if (((ChunkCoord.x / 2 + ChunkCoord.z / 2) ) % 2 == 0)
        //     return 0;
        // else
        //     return 1;
        Profiler.BeginSample("Find Biome");
        // float biomeHeight = CalcualteOceanHeight(ChunkCoord) * 0.7f + CalcualteBiomeHeight(ChunkCoord) * 0.3f;
        if (CalcualteOceanHeight(ChunkCoord) <= 0.5)
            return 0;
        
        float biomeHeight = CalcualteBiomeHeight(ChunkCoord);
        Profiler.EndSample();

        for (int i = 0; i < Biomes.Count; i++)
        {
            if (Biomes[i].BiomeHeightThresholdMax >= biomeHeight &&
                Biomes[i].BiomeHeightThresholdMin <= biomeHeight)
                return i;
        }
        Debug.LogWarning("Cant find chunk for that height");
        return 0;
    }

    private float CalcualteBiomeHeight(int3 ChunkCoord)
    {
        return noise.cnoise(new float2(ChunkCoord.x, ChunkCoord.z) * BiomeDensity) / 2 + 0.5f;
    }

    private float CalcualteOceanHeight(int3 ChunkCoord)
    {
        return noise.cnoise(new float2(ChunkCoord.x, ChunkCoord.z) * OceanDensity) / 2 + 0.5f;
    }

    private NativeArray<int3> _biomeNeighbours;

    // TODO: Remove Allocator.Persistent
    public ChunkGeneraionBiomes GetChunkGeneraionBiomes(IWorld world, int3 ChunkCoord)
    {
        if (_biomeNeighbours == null || _biomeNeighbours.Length >= 0)
            _biomeNeighbours = new NativeArray<int3>(VoxelData.BiomeNeighbours, Allocator.Persistent);
        ChunkGeneraionBiomes chunkGeneraionBiomes = new ChunkGeneraionBiomes
        {
            ChunkSize = VoxelData.ChunkSize,
            ChunkCoords = ChunkCoord,
            NeighbourBiomeIds = new NativeArray<int>(8, Allocator.Persistent),
            BiomeNeighbours = _biomeNeighbours,
            Biomes = GetBiomes(),
            Center = GetBiomeIndex(ChunkCoord),
            Lodes = GetBiomeLodes(GetBiomeIndex(ChunkCoord)),

            WaterLevel = WaterLevel
        };

        for (int i = 0; i < 8; i++)
        {
            chunkGeneraionBiomes.NeighbourBiomeIds[i] = world.GetOrCreateChunk(ChunkCoord + VoxelData.BiomeNeighbours[i]).GetBiomeIndex();
        }

        return chunkGeneraionBiomes;
    }

    public void Destroy()
    {
        foreach (var item in Biomes)
        {
            item.Destroy();
        }
        try { _biomeStructs.Dispose(); } catch { }
        try { _biomeNeighbours.Dispose(); } catch { }
    }
}