using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Lode", menuName = "CustomObjects/Lode")]
public class Lode : ScriptableObject
{
    public new string name;
    public int blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;

    public LodeStruct GetLodeStruct()
    {
        return new LodeStruct
        {
            blockID = blockID,
            maxHeight = maxHeight,
            minHeight = minHeight,
            noiseOffset = noiseOffset,
            scale = scale,
            threshold = threshold
        };
    }
}

public struct LodeStruct
{
    public int blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;

    public bool CheckForLode(float3 position)
    {
        return noise.snoise((position + noiseOffset) * scale) > threshold;
    }
}