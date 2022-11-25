using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;

public class RayCasting
{
    private static List<GameObject> markers = new List<GameObject>();

    private static GameObject CreateGameObject(IWorld world, float3 pos, int a, float size = 1.01f, float displacement = 0.5f)
    {
        var result = new GameObject();
        var _meshFilter = result.AddComponent<MeshFilter>();
        var _meshRenderer = result.AddComponent<MeshRenderer>();

        _meshFilter.mesh = world.GetBlockTypesList().Mesh;
        if (a == 0)
            _meshRenderer.material = world.GetBlockTypesList().MTest;
        else
            _meshRenderer.material = world.GetBlockTypesList().MTest2;

        result.transform.position = pos + new float3(displacement, displacement, displacement);
        result.transform.localScale = new Vector3(size, size, size);

        return result;
    }

    private static int FindSmallest(float3 position, float3 displacement, float3[] collisions, bool3 isLimited)
    {
        float3 smallestDistanceCollision = 0;
        float smallestDistance = 0;
        int smallestIndex = -1;
        bool isSet = false;
        float[] distance = new float[3];
        float3[] diff = new float3[3];

        for (int i = 0; i < 3; i++)
        {
            if (isLimited[i]) continue;
            diff[i] = collisions[i] - position;
            diff[i][i] += displacement[i];
            distance[i] = math.length(diff[i]);
            if (smallestDistance > distance[i] || !isSet) 
            {
                isSet = true;
                smallestDistance = distance[i];
                smallestDistanceCollision = collisions[i];
                smallestIndex = i;
            }
        }

        return smallestIndex;
    }

    public static bool3 Collide(IWorld world, float3 position, float3 vector, out float3 collision)
    {
        // foreach (GameObject item in markers)
        //     MonoBehaviour.Destroy(item);
        // markers.Clear();

        bool3 result = false;
        collision = 0;
        float3[] collisions = new float3[3];
        bool3 isLimited = false;
        int3 currentCheckedDistance = 1;
        int3 diff = (int3)math.abs(math.floor(vector + position) - math.floor(position));

        float3 vectorNormalized = math.normalize(vector);
        float e = 0.0001f;
        float3 displacement = math.select(-e, 1f + e, math.sign(vector) < 0);

        for (int i = 0; i < 3; i++)
            if (diff[i] < 1) isLimited[i] = true;
            else             collisions[i] = GetPointOnAxis(position, vectorNormalized, i, 1);

        while(math.any(!isLimited))
        {
            int smallestAxis = FindSmallest(position, displacement, collisions, isLimited);
            bool test = IsBlockSolid(world, (int3)math.floor(collisions[smallestAxis]));
            if (!test)
            {
                // markers.Add(CreateGameObject(world, (int3)math.floor(collisions[smallestAxis]), 0));
                if (++currentCheckedDistance[smallestAxis] < diff[smallestAxis])
                {
                    collisions[smallestAxis] = GetPointOnAxis(
                        position, 
                        vectorNormalized, 
                        smallestAxis, 
                        currentCheckedDistance[smallestAxis]
                    );
                }
                else
                {
                    isLimited[smallestAxis] = true;
                }
            }
            else   
            {
                collision = collisions[smallestAxis];
                collision[smallestAxis] += displacement[smallestAxis];
                result[smallestAxis] = true;
                markers.Add(CreateGameObject(world, collision, 1, 0.1f, 0));
                return result;
            }
        }

        return result;
    }

    public static float3 CollideAndSlide(IWorld world, float3 position, float3 vector)
    {
        foreach (GameObject item in markers)
            MonoBehaviour.Destroy(item);
        markers.Clear();
        float3 collision = position;
        float3 result = vector;
        int i = 0;
        while (i++ < 3)
        {
            bool3 collideResult = Collide(world, collision, vector, out collision);
            if (math.all(!collideResult)) return result;
            vector = math.select(vector, 0, collideResult);
            result = math.select(result, math.floor(collision) - math.floor(position), collideResult);
        }
        return result;
    }

    public static bool2 FindCollision(IWorld world, float3 position, float3 vector, out float3 collision, out float3 blockBefore)
    {
        // foreach (GameObject item in markers)
        //     MonoBehaviour.Destroy(item);
        // markers.Clear();

        float3[] collisions = new float3[3];
        bool3 isLimited = false;
        int3 currentCheckedDistance = 1;
        int3 diff = (int3)math.abs(math.floor(vector + position) - math.floor(position));

        float3 vectorNormalized = math.normalize(vector);
        float3 displacement = math.select((int3)0, 1, math.sign(vector) < 0);

        for (int i = 0; i < 3; i++)
            if (diff[i] < 1) isLimited[i] = true;
            else             collisions[i] = GetPointOnAxis(position, vectorNormalized, i, 1);

        bool2 result = false;
        blockBefore = 0;
        collision = 0;

        while(math.any(!isLimited))
        {
            int smallestAxis = FindSmallest(position, displacement, collisions, isLimited);
            bool test = IsBlockSolid(world, (int3)math.floor(collisions[smallestAxis]));
            if (!test)
            {
                blockBefore = collisions[smallestAxis];
                // markers.Add(CreateGameObject(world, (int3)math.floor(collisions[smallestAxis]), 0));
                if (++currentCheckedDistance[smallestAxis] < diff[smallestAxis])
                {
                    collisions[smallestAxis] = GetPointOnAxis(
                        position, 
                        vectorNormalized, 
                        smallestAxis, 
                        currentCheckedDistance[smallestAxis]
                    );
                    result.y = true;
                }
                else
                {
                    isLimited[smallestAxis] = true;
                }
            }
            else   
            {
                // isLimited[smallestAxis] = true;
                result.x = true;
                collision = collisions[smallestAxis];
                return result;
            }
        }

        return false;
    }

    private static float3 GetPointOnAxis(float3 position, float3 vectorNormalized, int axis, int index)
    {
        float3 a = vectorNormalized;
        float3 b = position;

        if (math.sign(a)[axis] < 0) index -= 1;

        float t = math.sign(a)[axis] * index + math.floor(position)[axis];
        float w = (t - b[axis]) / a[axis];

        if (math.sign(a)[axis] < 0) t -= 1;
        
        float3 p = 0;
        for (int j = 0; j < 3; j++)
            p[j] = axis == j ? t : w * a[j] + b[j];
        
        return p;
    }

    private static List<float3> GetBlocksToCheck(float3 position, float3 vector)
    {
        List<float3> points = new List<float3>();

        int3 diff = (int3)math.abs(math.floor(vector + position) - (int3)math.floor(position));

        float3 a = math.normalize(vector);
        float3 b = position;
        // (x - bx) / ax = (y - by) / ay = (z - bz) / az
        for (int axis = 0; axis < 3; axis++)
        {
            for (int i = 1; i < diff[axis]; i++)
            {
                int index = i;
                if (math.sign(a)[axis] < 0) index -= 1;

                float t = math.sign(a)[axis] * index + math.floor(position)[axis];
                float w = (t - b[axis]) / a[axis];

                if (math.sign(a)[axis] < 0) t -= 1;
                
                float3 p = 0;
                for (int j = 0; j < 3; j++)
                    p[j] = axis == j ? t : w * a[j] + b[j];

                points.Add(p);
            }
        }

        points = points
            .OrderBy(e => math.floor(e.x))
            .OrderBy(e => math.floor(e.y))
            .OrderBy(e => math.floor(e.z))
            .ToList();
        return points;
    }

    // public static bool2 FindCollision2(IWorld world, float3 position, float3 vector, out float3 collision, out float3 blockBefore)
    // {
    //     List<float3> points = GetBlocksToCheck(position, vector);

    //     foreach (GameObject item in markers)
    //     {
    //         MonoBehaviour.Destroy(item);
    //     }
    //     markers.Clear();

    //     for (int i = 0; i < points.Count; i++)
    //     {
    //         int3 p = (int3)math.floor(points[i]);
    //         if (IsBlockSolid(world, p))
    //         {
    //             markers.Add(CreateGameObject(world, p, 1));
    //             // bool2 result = true;
    //             // collision = points[i];
    //             // if (i != 0) blockBefore = points[i - 1];
    //             // else 
    //             // {
    //             //     blockBefore = math.floor(position);
    //             //     result = false;
    //             // }
    //             // return result;
    //         }
    //         else
    //             markers.Add(CreateGameObject(world, p, 0));
    //     }
    //     collision = 0;
    //     blockBefore = 0;
    //     return false;
    // }

    public static bool FindPosToDestroy(IWorld world, float3 position, float3 vector, out float3 blockPosition)
    {
        return FindCollision(world, position, vector, out blockPosition, out var t).x;
    }

    public static bool FindPosToPlace(IWorld world, float3 position, float3 vector, out float3 blockPosition)
    {
        return FindCollision(world, position, vector, out var t, out blockPosition).y;
    }

    private static bool IsBlockSolid(IWorld world, int3 position)
    {
        int blockId = 0;
        if (world.TryGetBlock(position, ref blockId))
        {
            return world.GetBlockTypesList().areSolid[blockId];
        }
        return false;
    }
}
