using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

// public class ChunkOld : IChunk
// {
//     public int3 coordinates;
//     public int3 Coordinates => coordinates;
//     private static int3 Size { get => VoxelData.ChunkSize; }
//     public float3 ChunkPosition => new float3(coordinates.x, coordinates.y, coordinates.z) * Size;
//     public float3 GetChunkPosition() => ChunkPosition;

//     private WorldClass world;
//     private ChunkGeneratorOld chunkGenerator;
//     public ChunkGeneratorOld ChunkGenerator => chunkGenerator;
//     public bool ready = false;

//     private ChunkRenderer chunkRenderer;

//     public ChunkOld(WorldClass _world, int3 _position)
//     {
//         coordinates = _position;
//         world = _world;

//         chunkGenerator = new ChunkGeneratorOld(this, world, world.GetBiome(coordinates));

//         chunkRenderer = new ChunkRenderer(this, world);
//         chunkRenderer.Update();
//     }
    
//     public ChunkOld(WorldClass _world, ChunkData data)
//     {
//         coordinates = new int3(data.Coords[0], data.Coords[1], data.Coords[2]);
//         world = _world;

//         chunkGenerator = new ChunkGeneratorOld(this, world, world.GetBiome(coordinates));
//     }
    
//     public void Destroy() 
//     {
//         chunkRenderer.Destroy();
//         chunkGenerator.Destroy();
//     }
//     public void Hide() => chunkGenerator.Hide();
//     public void SetNeighbours(ChunkNeighbours chunkNeighbours) => chunkGenerator.SetNeighbours(chunkNeighbours);
//     public bool CanEditChunk() => chunkGenerator.CanEditChunk();
//     public void ForceUpdate() => chunkGenerator.ForceUpdate();
//     public void NeighbourDepecency(int value) => chunkGenerator.NeighbourDepecency(value);
//     public int GetIndex(int3 position) => position.x + (position.y + position.z * Size.y) * Size.x;

//     public int GetBlock(int3 position)
//     {
//         if (!ready) return 1;
//         return chunkGenerator.GetBlock(position);
//     }
//     public int SetBlock(int3 position, int value) => chunkGenerator.SetBlock(position, value);
//     // private void PropagateStrucutre(int3 lPosition, int structureId)
//     // {
//     //     if (chunkGenerator.neighbours == null)
//     //         return;
//     //     for (int i = 0; i < 3; i++)
//     //     {
//     //         if (math.any(((lPosition + world.Structures[structureId].Size3) * VoxelData.axisArray[i]) >= Size) &&
//     //             chunkGenerator.neighbours[VoxelData.axisArray[i]] != null)
//     //         {
//     //             chunkGenerator.neighbours[VoxelData.axisArray[i]].CreateStructure(
//     //                 lPosition - Size * VoxelData.axisArray[i],
//     //                 structureId);
//     //         }
//     //     }
//     // }
//     // public void CreateStructure(int3 lPosition, int structureId)
//     // {
//     //     PropagateStrucutre(lPosition, structureId);
//     //     chunkGenerator.CreateStructure(lPosition, structureId);
//     // }
//     public NativeArray<int> GetBlocks() => chunkGenerator.GetBlocks();
//     public NativeArray<int> GetMeshBlocks() => chunkGenerator.GetMeshBlocks();

//     public bool TryPlaceBlock(int3 position, int blockID)
//     {
//         if (!world.blockTypesList.areReplacable[GetBlock(position)]) return false;

//         SetBlock(position, blockID);
//         return true;
//     }

//     public ChunkNeighbourData GetNeighbourData()
//     {
//         ChunkNeighbourData data = new ChunkNeighbourData();

//         for (int i = 0; i < 6; i++)
//         {
//             data.ChunkNeighbourDataArray[i] = new NativeArray<int>();
//         }

//         return data;
//     }
    
//     public void ReleaseNeighbourData()
//     {
        
//     }

//     public void Render()
//     {
//         chunkRenderer.Render();
//     }

//     public BiomeAttributesStruct GetBiome()
//     {
//         return world.GetBiome(Coordinates);
//     }

//     public void Generate()
//     {
        
//     }

//     public int3 GetChunkCoordinates()
//     {
//         return Coordinates;
//     }

//     public bool TryGetBlock(int3 position, out int blockId)
//     {
//         blockId = 0;
//         return false;
//     }

//     public bool TrySetBlock(int3 position, int placedBlockId, out int replacedBlockId)
//     {
//         replacedBlockId = 0;
//         return chunkGenerator.TrySetBlock(position, placedBlockId, ref replacedBlockId);
//     }

//     private NativeArray<int> sharedData;
//     private int sharedDataUsed = 0;

//     public NativeArray<int> GetSharedData()
//     {
//         if (sharedDataUsed <= 0)
//         {
//             GetBlocks().CopyTo(sharedData);
//         }
//         sharedDataUsed++;
//         return sharedData;
//     }

//     public void ReleaseSharedData()
//     {
//         sharedDataUsed--;
//         if (sharedDataUsed <= 0)
//             sharedData.Dispose();
//     }

//     public void Save()
//     {
        
//     }

//     public void Update()
//     {
//         chunkRenderer.Update();
//     }
//     public bool CanAccess()
//     {
//         throw new System.NotImplementedException();
//     }
// }
