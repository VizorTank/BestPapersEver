using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

[System.Serializable]
public class WorldStaticData
{
    public int RenderDistance = 16;
    public int3 WorldSizeInChunks = new int3(64, 8, 64);
    public BlockTypesList BlockTypesList;
    public List<Structure> Structures = new List<Structure>();
    public List<BiomeAttributes> BiomeAttributes = new List<BiomeAttributes>();
    public int WaterLevel = 64;
}