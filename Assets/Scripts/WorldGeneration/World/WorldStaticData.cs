using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WorldStaticData", menuName = "CustomObjects/World Static Data")]
public class WorldStaticData : ScriptableObject
{
    public int RenderDistance = 4;
    public int WorldHeightInChunks = 8;
    public BlockTypesList BlockTypesList;
    public List<Structure> Structures = new List<Structure>();
    public WorldBiomesList WorldBiomes;
}