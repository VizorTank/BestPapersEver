using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CharacterCollider
{
    private IWorld _world;
    private float3[][] _points;
    private float3[] _displacement;
    public CharacterCollider(IWorld world, float3 size)
    {
        _world = world;
        CreatePoints();
    }

    private void CreatePoints()
    {
        int3 count = new int3(2, 3, 2);
        float3 size = new float3(0.7f, 1.7f, 0.7f);
        float3 size2 = size / 2;
        _points = new float3[][] {
            // Plane X
            new float3[] { 
                new float3(       0, -size2.y, -size2.z),
                new float3(       0, -size2.y,  size2.z),
                new float3(       0,        0, -size2.z),
                new float3(       0,        0,  size2.z),
                new float3(       0,  size2.y, -size2.z),
                new float3(       0,  size2.y,  size2.z)
            },
            // Plane Y
            new float3[] { 
                new float3(-size2.x,        0, -size2.z),
                new float3(-size2.x,        0,  size2.z),
                new float3( size2.x,        0,  size2.z),
                new float3( size2.x,        0, -size2.z)
            },
            // Plane Z
            new float3[] { 
                new float3(-size2.x, -size2.y,        0),
                new float3( size2.x, -size2.y,        0),
                new float3(-size2.x,        0,        0),
                new float3( size2.x,        0,        0),
                new float3(-size2.x,  size2.y,        0),
                new float3( size2.x,  size2.y,        0)
            }
        };
        
        _displacement = new float3[] {
            new float3(size2.x,       0,       0),
            new float3(      0, size2.y,       0),
            new float3(      0,       0, size2.z)
        };
    }

    private float3 CollideAndSlide(float3 position, float3 vector)
    {
        float3 sign = math.sign(vector);
        float3 result = vector;

        for (int axis = 0; axis < 3; axis++)
        {
            for (int i = 0; i < _points[axis].Length; i++)
            {
                float3 r = RayCasting.CollideAndSlide(
                    _world, 
                    position + _points[axis][i] + _displacement[axis] * sign, 
                    vector
                );
                result = math.min(result, r);
            }
        }

        return result;
    }

}
