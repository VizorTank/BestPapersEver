

using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ChunkManipulator
{
    private IChunk _chunk;
    private IWorld _world;
    private NativeArray<int> _blocksId;

    private int _sharedCount;
    private NativeArray<int> _sharedData;
    

    private ChunkGenerator2 _chunkGenerator;
    
    public ChunkManipulator(IChunk chunk, IWorld world)
    {
        _chunk = chunk;
        _world = world;

        _chunkGenerator = new ChunkGenerator2();
    }

    public void Destroy()
    {
        try { _blocksId.Dispose(); } catch { }
        try { _sharedData.Dispose(); } catch { }
    }

    public void GenerateBlocks()
    {
        if (CanAccess()) return;
        if (_chunkGenerator.GenerateBlocks(_chunk, _world, out NativeArray<int> generatedBlocks))
        {
            _blocksId = generatedBlocks;
            _chunk.UpdateNeighbours();
        }
    }

    public bool CanAccess() => _blocksId != null && _blocksId.Length != 0;

    #region GetDataForNeighbours
    public NativeArray<int> GetSharedData()
    {
        if (_sharedCount <= 0)
        {
            _sharedData = new NativeArray<int>(_blocksId, Allocator.Persistent);
            _sharedCount = 0;
        }
        _sharedCount++;
        return _sharedData;
    }

    public void ReleaseSharedData()
    {
        _sharedCount--;
        if (_sharedCount <= 0)
        {
            try { _sharedData.Dispose(); } catch { }
        }
    }
    #endregion


    #region AccessChunk
    public int GetBlock(int3 blockPosition) => _blocksId[VoxelData.GetIndex(blockPosition)];
    public NativeArray<int> GetBlocks() => _blocksId;

    public void SetBlock(int3 blockPosition, int blockId)
    {
        int p = VoxelData.GetIndex(blockPosition);
        _blocksId[p] = blockId;
        int x = p % VoxelData.ChunkSize.x;
        int y = p / VoxelData.ChunkSize.x % VoxelData.ChunkSize.y;
        int z = p / VoxelData.ChunkSize.x / VoxelData.ChunkSize.y;
        _chunk.Update();
    }  
    #endregion

    #region Save&Load
    public void Load(ChunkData data)
    {
        if (data.BlockIds != null)
        {
            _blocksId = new NativeArray<int>(data.BlockIds, Allocator.Persistent);
            _chunk.Update();
        }
    }
    #endregion


    public void CreateStructures(List<StructureToLoad> structures)
    {
        if (structures.Count > 0)
        {
            foreach (StructureToLoad structureToLoad in structures)
            {
                PlaceStructure(structureToLoad.position, structureToLoad.id);
            }
            _chunk.Update();
        }
    }

    private void PlaceStructure(int3 structurePosition, int structureId)
    {
        Structure structure = _world.GetStructure(structureId);
        int3 sSize = structure.Size;
        int3 Size = VoxelData.ChunkSize;

        for (int x = math.max(structurePosition.x, 0); x < math.min(sSize.x + structurePosition.x, Size.x); x++)
        {
            for (int y = math.max(structurePosition.y, 0); y < math.min(sSize.y + structurePosition.y, Size.y); y++)
            {
                for (int z = math.max(structurePosition.z, 0); z < math.min(sSize.z + structurePosition.z, Size.z); z++)
                {
                    int3 sBlockPos = new int3(x, y, z) - structurePosition;
                    int blockId = _blocksId[VoxelData.GetIndex(new int3(x, y, z))];
                    int structureBlockId = structure.GetValue(new int3(sBlockPos.x, sBlockPos.y, sBlockPos.z));
                    if (_world.GetBlockTypesList().areReplacable[blockId] ||
                        !_world.GetBlockTypesList().areReplacable[structureBlockId])
                        if (_world.GetBlockTypesList().blockTypes[blockId].isReplaceableByStructure)
                            _blocksId[VoxelData.GetIndex(new int3(x, y, z))] = structureBlockId;
                }
            }
        }
    }
}

public class StructureToLoad
{
    public int3 position;
    public int id;
}