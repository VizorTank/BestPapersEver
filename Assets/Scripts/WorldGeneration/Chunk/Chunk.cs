using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class Chunk : IChunk
{
    private IWorld _world;

    private ChunkRenderer _renderer = null;
    private ChunkManipulator _manipulator = null;

    private int3 _chunkCoordinates;
    private bool _isDestroyed = false;
    private bool _isCreated = false;
    private List<StructureToLoad> StructuresToLoad = new List<StructureToLoad>();

    private ChunkNeighbours _neighbours = new ChunkNeighbours();
    private int _biomeIndex = -1;


    public Chunk(IWorld world, int3 chunkCoordinates)
    {
        _world = world;
        _chunkCoordinates = chunkCoordinates;
    }

    // public Chunk(IWorld world, ChunkData data)
    // {
    //     _world = world;
    //     _chunkCoordinates = chunkCoordinates;
    // }

    public void Init()
    {
        _renderer = new ChunkRenderer(this, _world);
        _manipulator = new ChunkManipulator(this, _world);
        _isCreated = true;
    }

    public void Init(ChunkData data)
    {
        Init();
        _manipulator.Load(data);
    }
    
    public void Destroy()
    {
        _isDestroyed = true;
        _renderer.Destroy();
        _manipulator.Destroy();
    }

    public bool IsDestroyed() 
    {
        return _isDestroyed;
    }

    public BiomeAttributesStruct GetBiome()
    {
        return _world.GetBiome(GetBiomeIndex());
    }

    public NativeArray<int> GetBlocks()
    {
        return _manipulator.GetBlocks();
    }

    public int GetBiomeIndex()
    {
        if (_biomeIndex < 0)
            _biomeIndex = _world.GetBiomeIndex(GetChunkCoordinates());
        
        return _biomeIndex;
    }

    public int3 GetChunkCoordinates()
    {
        return _chunkCoordinates;
    }

    private bool TryGetNeigbours(ref ChunkNeighbours chunkNeighbours)
    {
        Profiler.BeginSample("Get Neighbours");
        bool result = _world.TryGetNeighbours(GetChunkCoordinates(), ref chunkNeighbours);
        Profiler.EndSample();
        return result;
    }

    public NativeArray<int> GetSharedData()
    {
        return _manipulator.GetSharedData();
    }

    public void ReleaseSharedData()
    {
        _manipulator.ReleaseSharedData();
    }

    public void Render()
    {
        // Profiler.BeginSample("Check and generate blocks");
        if (_isDestroyed || _manipulator == null) return;
        _manipulator.GenerateBlocks();
        if (!CanAccess()) return;
        // Profiler.EndSample();

        // Profiler.BeginSample("Create structures");
        _manipulator.CreateStructures(StructuresToLoad);
        StructuresToLoad.Clear();
        // Profiler.EndSample();
        if (!_renderer.RequireProcessing()) return;
        if (!_renderer.CanAccess()) return;


        Profiler.BeginSample("Render chunk");
        Profiler.BeginSample("Get N");
        bool canRender = true;
        if (!_neighbours.IsValid())
            canRender = TryGetNeigbours(ref _neighbours);
        Profiler.EndSample();

        if (canRender)
        {
            // Profiler.BeginSample("Render chunk");
            _renderer.Render(_neighbours);
            // Profiler.EndSample();
        }
        else
        {
            // Profiler.BeginSample("Hide chunk");
            Hide();
            Update();
            // Profiler.EndSample();
        }
        Profiler.EndSample();
    }

    public bool TryGetBlock(int3 position, out int blockId)
    {
        blockId = 0;
        if (!CanAccess()) return false;
        blockId = _manipulator.GetBlock(position);
        return true;
    }

    public bool TrySetBlock(int3 position, int placedBlockId, out int replacedBlockId)
    {
        replacedBlockId = 0;
        if (!CanAccess()) return false;
        replacedBlockId = _manipulator.GetBlock(position);
        Debug.Log($"Placed: {position.x}, {position.y}, {position.z}");
        _manipulator.SetBlock(position, placedBlockId);
        UpdateNeighbourChunks(position);
        return true;
    }

    public void UpdateNeighbourChunks(int3 blockPosition)
    {
        if (!math.any(blockPosition == 0) || math.any(blockPosition == VoxelData.ChunkSize - 1)) return;

        for (int i = 0; i < 3; i++)
        {
            int3 tmp = blockPosition * VoxelData.axisArray[i];
            for (int j = 0; j < 2; j++)
            {
                int3 border = (VoxelData.axisArray[i] + VoxelData.voxelNeighbours[i * 2 + j])  * VoxelData.ChunkSize / 2;
                if (math.all(tmp == border)) 
                {
                    IChunk chunk = _world.GetChunk(GetChunkCoordinates() + VoxelData.voxelNeighbours[i * 2 + j]);
                    if (chunk != null)
                    {
                        chunk.Update();
                    }
                }
            }
        }

        if (blockPosition.x < 0) _world.GetChunk(GetChunkCoordinates() - new int3(1, 0, 0));
    }

    public float3 GetChunkPosition()
    {
        return new float3(GetChunkCoordinates()) * VoxelData.ChunkSize;
    }

    public void Update()
    {
        _renderer.Update();
    }
    public void Hide()
    {
        if (CanAccess())
            _renderer.Unload();
    }

    public bool CanAccess()
    {
        return _manipulator != null && _manipulator.CanAccess() && !_isDestroyed;
    }

    // public bool CanCreateStructure(int3 structurePosition, Structure structure)
    // {
    //     int3 structureEndPosition = structure.Size3 + structurePosition;
    //     for (int i = 0; i < 3; i++)
    //     {
    //         if (math.any(structureEndPosition * VoxelData.axisArray[i] > VoxelData.ChunkSize))
    //         {
    //             IChunk chunk = _world.GetChunk(GetChunkCoordinates() + VoxelData.axisArray[i]);
    //             _world.CreateStructure(structurePosition - VoxelData.axisArray[i] * VoxelData.ChunkSize, structure);
    //         }
    //     }

    //     return true;
    // }

    public void CreateStructure(int3 structurePosition, int structureId)
    {
        PropagateStrucutre(structurePosition, structureId);
        // _manipulator.CreateStructure(structurePosition, structureId);
        // Debug.Log("Created structure");
        StructuresToLoad.Add(new StructureToLoad {
            position = structurePosition,
            id = structureId
        });
    }

    private void PropagateStrucutre(int3 structurePosition, int structureId)
    {
        Structure structure = _world.GetStructure(structureId);
        // ChunkNeighbours neighbours = new ChunkNeighbours();
        // if (!_world.TryGetNeighbours(GetChunkCoordinates(), ref neighbours)) 
        // {
        //     return false;
        // }
        // int3 structureEnd = structure.Size3 + structurePosition;

        // for (int i = 0; i < 6; i++)
        // {
        //     if (i == 0 || i == 3 || i == 4) continue;
        //     if (math.any(structureEnd * VoxelData.voxelNeighbours[i] > VoxelData.ChunkSize))
        //     {
        //         neighbours[i].CreateStructure(structurePosition - VoxelData.voxelNeighbours[i] * VoxelData.ChunkSize, structure);
        //     }
        // }

        int3 structureEndPosition = structure.Size3 + structurePosition;
        for (int i = 0; i < 3; i++)
        {
            if (math.any(structureEndPosition * VoxelData.axisArray[i] > VoxelData.ChunkSize))
            {
                // IChunk chunk = _world.GetChunk(GetChunkCoordinates() + VoxelData.axisArray[i]);
                // _world.CreateStructure(structurePosition - VoxelData.axisArray[i] * VoxelData.ChunkSize + GetChunkPosition(), structureId);
                _world.CreateStructure(GetChunkCoordinates() + VoxelData.axisArray[i], structurePosition - VoxelData.axisArray[i] * VoxelData.ChunkSize, structureId);
            }
        }
    }
}