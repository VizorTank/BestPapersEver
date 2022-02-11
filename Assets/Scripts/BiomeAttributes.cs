using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "CustomObjects/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    public string biomeName;
    public int solidGroundHeight;
    public int terrainHeight;
    public float terrainSize;

    public Lode[] lodes;
}

[System.Serializable]
public class Lode
{
    public string name;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}