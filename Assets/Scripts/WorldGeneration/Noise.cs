using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(Vector2 position, float offset, float scale)
    {
        return Mathf.PerlinNoise((position.x + 0.1f) / Chunk.Size.x * scale + offset,
                                    (position.y + 0.1f) / Chunk.Size.y * scale + offset);
    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        float x = (position.x + offset + 0.1f) * scale;
        float y = (position.y + offset + 0.1f) * scale;
        float z = (position.z + offset + 0.1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float CA = Mathf.PerlinNoise(z, x);

        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float AC = Mathf.PerlinNoise(x, z);

        if ((AB + AC + BA + BC + CA + CB) / 6f > threshold)
            return true;
        else
            return false;
    }
}