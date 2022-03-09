using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkV2 : ChunkCore
{
    int[,,,] blockFaceCheck = new int[Size.x, Size.y, Size.z, 6];

    public ChunkV2(Vector3Int _position, WorldClass _world) : base(_position, _world)
    {
        PopulateVoxelMap2();
    }

    void PopulateVoxelMap2()
    {
        ComputeBuffer buffer = new ComputeBuffer(Size.x * Size.y * Size.z, sizeof(int));
        int kernelIndex = world.generateWorldShader.FindKernel("CSMain");
        buffer.SetData(voxelMap);
        world.generateWorldShader.SetBuffer(kernelIndex, "VoxelData", buffer);

        world.generateWorldShader.SetInts("ChunkPosition", new int[] { (int)ChunkPosition.x, (int)ChunkPosition.y, (int)ChunkPosition.z });
        world.generateWorldShader.SetInts("ChunkSize", new int[] { Size.x, Size.y, Size.z });
        world.generateWorldShader.SetFloats("WorldSizeInVoxels", new float[] { WorldClass.WorldSizeInVoxels.x, WorldClass.WorldSizeInVoxels.y, WorldClass.WorldSizeInVoxels.z });
        world.generateWorldShader.SetFloat("NoiseOffset", world.NoiseOffset);
        world.generateWorldShader.SetFloat("TerrainSize", world.biome.terrainSize / 50);
        world.generateWorldShader.SetInt("TerrainHeight", world.biome.terrainHeight);
        world.generateWorldShader.SetInt("SolidGroundHeight", world.biome.solidGroundHeight);

        world.generateWorldShader.Dispatch(kernelIndex, Size.x / 8, Size.y / 8, Size.z / 8);

        buffer.GetData(voxelMap);
        buffer.Release();
    }

    public override void DrawChunk()
    {
        base.DrawChunkBefore();
        CreateMeshData2();
        AddvoxelsToMesh();
        base.DrawChunkAfter();
    }

    void CreateMeshData2()
    {
        ComputeBuffer blockTypeBuffer = new ComputeBuffer(world.blockTypes.Length, sizeof(int));
        ComputeBuffer blockListBuffer = new ComputeBuffer(Size.x * Size.y * Size.z, sizeof(int));
        ComputeBuffer blockFaceCheckBuffer = new ComputeBuffer(Size.x * Size.y * Size.z * 6, sizeof(int));

        int kernelIndex = world.generateWorldShader.FindKernel("CSMain");

        int[] blockType = new int[world.blockTypes.Length];
        for (int i = 0; i < world.blockTypes.Length; i++)
        {
            blockType[i] = world.blockTypes[i].isSolid ? 1 : 0;
        }
        blockTypeBuffer.SetData(blockType);

        blockListBuffer.SetData(voxelMap);

        blockFaceCheckBuffer.SetData(blockFaceCheck);

        world.createMeshShader.SetBuffer(kernelIndex, "BlockList", blockListBuffer);
        world.createMeshShader.SetBuffer(kernelIndex, "BlockTypeIsSolid", blockTypeBuffer);
        world.createMeshShader.SetBuffer(kernelIndex, "BlockFaceCheck", blockFaceCheckBuffer);

        world.createMeshShader.SetInts("ChunkSize", new int[] { Size.x, Size.y, Size.z });

        world.createMeshShader.Dispatch(kernelIndex, Size.x / 8, Size.y / 8, Size.z / 8);

        blockFaceCheckBuffer.GetData(blockFaceCheck);

        blockFaceCheckBuffer.Release();
        blockListBuffer.Release();
        blockTypeBuffer.Release();

        
        
        //for (int y = 0; y < Size.y; y++)
        //{
        //    for (int x = 0; x < Size.x; x++)
        //    {
        //        for (int z = 0; z < Size.z; z++)
        //        {
        //            if (world.blockTypes[voxelMap[x, y, z]].isSolid)
        //                AddVoxelDataToChunks2(new Vector3(x, y, z));
        //        }
        //    }
        //}
    }

    protected override bool IsVoxelSolid(int x, int y, int z)
    {
        return world.blockTypes[voxelMap[x, y, z]].isSolid;
    }

    protected override bool CheckNeighbourVoxel(int x, int y, int z, int i)
    {
        return Convert.ToBoolean(blockFaceCheck[x, y, z, i]);
    }

    //void AddVoxelDataToChunks2(Vector3 position)
    //{
    //    for (int i = 0; i < 6; i++)
    //    {
    //        if (Convert.ToBoolean(blockFaceCheck[(int)position.x, (int)position.y, (int)position.z, i]))
    //        {
    //            AddVoxelFaceToMesh(position, i);
    //        }
    //    }
    //}
}
