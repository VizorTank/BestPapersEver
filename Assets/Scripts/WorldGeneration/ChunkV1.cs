using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class ChunkV1 : ChunkCore
{

    //static readonly ProfilerMarker profilerCreateMeshDataY = new ProfilerMarker("CreateMeshDataY");
    //profilerCreateMeshDataY.Begin();
    //profilerCreateMeshDataY.End();

    public ChunkV1(Vector3Int _position, WorldClass _world) : base(_position, _world)
    {
        PopulateVoxelMap();
    }

    public override void DrawChunk()
    {
        base.DrawChunkBefore();
        AddvoxelsToMesh();
        base.DrawChunkAfter();
    }

    protected override bool IsVoxelSolid(int x, int y, int z)
    {
        return world.blockTypes[voxelMap[x, y, z]].isSolid;
    }

    protected override bool CheckNeighbourVoxel(int x, int y, int z, int i)
    {
        return !CheckVoxel(new Vector3(x, y, z) + VoxelData.faceChecks[i]);
    }

    bool CheckVoxel(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        if (!isVoxelInChunk(x, y, z))
            return world.CheckForVoxel(position + ChunkPosition);

        return world.blockTypes[voxelMap[x, y, z]].isSolid;
    }

    bool isVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x >= Size.x ||
            y < 0 || y >= Size.y ||
            z < 0 || z >= Size.z)
            return false;
        return true;
    }

    void PopulateVoxelMap()
    {
        for (int y = 0; y < Size.y; y++)
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + ChunkPosition);
                }
            }
        }
    }

    //void CreateMeshData()
    //{
    //    for (int y = 0; y < Size.y; y++)
    //    {
    //        for (int x = 0; x < Size.x; x++)
    //        {
    //            for (int z = 0; z < Size.z; z++)
    //            {
    //                if (world.blockTypes[voxelMap[x, y, z]].isSolid)
    //                    AddVoxelDataToChunks(new Vector3(x, y, z));
    //            }
    //        }
    //    }
    //}

    //void AddVoxelDataToChunks(Vector3 position)
    //{
    //    for (int i = 0; i < 6; i++)
    //    {
    //        if (!CheckVoxel(position + VoxelData.faceChecks[i]))
    //        {
    //            AddVoxelFaceToMesh(position, i);
    //        }
    //    }
    //}
}