using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

// public class ChunkGeneratorOld
// {
//     private ChunkOld chunk;
//     private WorldClass world;

//     private int3 ChunkCoords;

//     private BlockTypesList blockTypes
//     {
//         get => world.blockTypesList;
//     }

//     private GameObject chunkObject;
//     private MeshRenderer meshRenderer;
//     private MeshFilter meshFilter;

//     // State machine
//     private ChunkGeneratorStates state;
//     private enum ChunkGeneratorStates
//     {
//         Updated,
//         RequiringUpdate,
//         GeneratingMap,
//         SettingBlocks,
//         CreatingClusters,
//         CheckingVisibility,
//         CreatingMesh,
//         LoadingMesh
//     }

//     // Blocks
//     private static int3 Size { get => VoxelData.ChunkSize; }
//     private NativeArray<int> blocksId;
//     private NativeArray<int> blocksForMeshGeneration;

//     // Clusters
//     private NativeList<ClusterCreationStruct> ClusterData;
    
//     private NativeArray<int> blocksClusterIdDatas;
//     /*private NativeList<int> clusterBlockIdDatas;
//     private NativeList<int3> clusterSizeDatas;
//     private NativeList<int3> clusterPositionDatas;
//     private NativeList<ClusterSidesVisibility> clusterSidesVisibilityData;*/

//     // Const
//     private NativeArray<int3> axisArray;

//     private NativeArray<int3> voxelNeighbours;
//     private NativeArray<float3> voxelVerts;
//     private int voxelTrisSize;
//     private NativeArray<int> voxelTris;
//     private NativeArray<float2> voxelUvs;
//     private NativeArray<int> triangleOrder;

//     private NativeArray<VertexAttributeDescriptor> layout;

//     private NativeArray<int3> clusterSides;

//     public ChunkNeighbours neighbours;
//     private ChunkNeighbourData ChunkNeighbourData;

//     // Jobs
//     public int usedByOtherChunks = 0;

//     //private bool updating;
//     //private bool requireUpdate;
//     //private bool readyForUpdate;
//     //private bool generatingBlockIds;
//     //private bool generatingClusters;
//     //private bool checkingVisibility;
//     //private bool generatingMesh;
//     private Mesh.MeshDataArray meshDataArray;
//     private JobHandle generatingBlockIdJobHandle;
//     private JobHandle generatingClustersJobHandle;
//     private JobHandle checkingVisibilityJobHandle;
//     private JobHandle generatingMeshJobHandle;

//     private BiomeAttributesStruct biome;

//     public ChunkGeneratorOld(ChunkOld _chunk, WorldClass _world, BiomeAttributesStruct _biome)
//     {
//         Init(_chunk, _world, _biome);

//         state = ChunkGeneratorStates.GeneratingMap;
//         GenerateBlockIdWithJobs();
//         generatingBlockIdJobHandle.Complete();
//         //generatingBlockIds = true;
//         //updating = true;
//     }

//     public ChunkGeneratorOld(ChunkOld _chunk, WorldClass _world, BiomeAttributesStruct _biome, ChunkData data)
//     {
//         Init(_chunk, _world, _biome);
        
//         blocksId = new NativeArray<int>(data.BlockIds, Allocator.Persistent);
        
//         state = ChunkGeneratorStates.RequiringUpdate;
//     }

//     public void Init(ChunkOld _chunk, WorldClass _world, BiomeAttributesStruct _biome)
//     {
//         chunk = _chunk;
//         world = _world;
//         biome = _biome;

//         //MeshInit();
        
//         //CreateConst();
//     }

//     private void MeshInit()
//     {
//         chunkObject = new GameObject();
//         meshFilter = chunkObject.AddComponent<MeshFilter>();
//         meshRenderer = chunkObject.AddComponent<MeshRenderer>();

//         chunkObject.transform.SetParent(world.transform);
//         ChunkCoords = chunk.Coordinates;
//         chunkObject.transform.position = Vector3.Scale(new Vector3(ChunkCoords.x, ChunkCoords.y, ChunkCoords.z), new Vector3(Size.x, Size.y, Size.z));
//         chunkObject.name = string.Format("Chunk {0}, {1}, {2}", ChunkCoords.x, ChunkCoords.y, ChunkCoords.z);

//     }

//     private void CreateConst()
//     {
//         axisArray = new NativeArray<int3>(VoxelData.axisArray, Allocator.Persistent);

//         voxelNeighbours = new NativeArray<int3>(VoxelData.voxelNeighbours, Allocator.Persistent);
//         voxelVerts = new NativeArray<float3>(VoxelData.voxelVerts, Allocator.Persistent);
//         voxelUvs = new NativeArray<float2>(VoxelData.voxelUvs, Allocator.Persistent);
//         voxelTris = new NativeArray<int>(VoxelData.voxelTris, Allocator.Persistent);
//         voxelTrisSize = VoxelData.voxelTrisSize;
//         triangleOrder = new NativeArray<int>(VoxelData.triangleOrder, Allocator.Persistent);

//         layout = new NativeArray<VertexAttributeDescriptor>(VoxelData.layoutVertex, Allocator.Persistent);

//         clusterSides = new NativeArray<int3>(VoxelData.clusterSidesArray, Allocator.Persistent);

//         blocksForMeshGeneration = new NativeArray<int>(Size.x * Size.y * Size.z, Allocator.Persistent);

//         blocksClusterIdDatas = new NativeArray<int>(Size.x * Size.y * Size.z, Allocator.Persistent);
//         /*clusterBlockIdDatas = new NativeList<int>(Allocator.Persistent);
//         clusterPositionDatas = new NativeList<int3>(Allocator.Persistent);
//         clusterSizeDatas = new NativeList<int3>(Allocator.Persistent);
//         clusterSidesVisibilityData = new NativeList<ClusterSidesVisibility>(Allocator.Persistent);*/

//         ClusterData = new NativeList<ClusterCreationStruct>(Allocator.Persistent);
//     }
//     public void Destroy()
//     {
//         DisposeArrays();

//         ChunkNeighbourData.Destroy();

//         UnityEngine.Object.Destroy(chunkObject);
//     }

//     private void DisposeArrays()
//     {
//         generatingBlockIdJobHandle.Complete();
//         generatingClustersJobHandle.Complete();
//         checkingVisibilityJobHandle.Complete();
//         generatingMeshJobHandle.Complete();

//         try { axisArray.Dispose(); } catch { }

//         try { voxelNeighbours.Dispose(); } catch { }
//         try { voxelVerts.Dispose(); } catch { }
//         try { voxelUvs.Dispose(); } catch { }
//         try { voxelTris.Dispose(); } catch { }
//         try { triangleOrder.Dispose(); } catch { }
//         try { layout.Dispose(); } catch { }

//         try { clusterSides.Dispose(); } catch { }

//         try { blocksId.Dispose(); } catch { }

//         try { blocksClusterIdDatas.Dispose(); } catch { }
//         /*try { clusterBlockIdDatas.Dispose(); } catch { }
//         try { clusterSizeDatas.Dispose(); } catch { }
//         try { clusterPositionDatas.Dispose(); } catch { }
//         try { clusterSidesVisibilityData.Dispose(); } catch { }*/
//         try { ClusterData.Dispose(); } catch { }
//         try { blocksForMeshGeneration.Dispose(); } catch { }

//         try { meshDataArray.Dispose(); } catch { }
//     }
    
//     public bool CanEditChunk() => true;
//     //public bool CanEditChunk() => !updating;
    
//     //public void ForceUpdate() => requireUpdate = true;

//     public int GetIndex(int3 position) => position.x + (position.y + position.z * Size.y) * Size.x;
    
//     #region Miscellaneous

//     #region SetBlock

    
//     public bool TrySetBlock(int3 position, int blockId, ref int replacedBlockId)
//     {
//         if (position.x < 0 || position.x >= Size.x ||
//             position.y < 0 || position.y >= Size.y ||
//             position.z < 0 || position.z >= Size.z) return false;

//         replacedBlockId = SetBlock(position, blockId);

//         return true;
//     }
    
//     public int SetBlock(int3 position, int value)
//     {
//         int oldBlockID = GetBlock(position);
//         blocksId[GetIndex(position)] = value;
//         ForceUpdate();
        
//         return oldBlockID;
//     }

//     #endregion

//     #region GetBlock

//     public bool TryGetBlock(int3 position, ref int blockId)
//     {
//         if (position.x < 0 || position.x >= Size.x ||
//             position.y < 0 || position.y >= Size.y ||
//             position.z < 0 || position.z >= Size.z) return false;

//         blockId = blocksId[GetIndex(position)];
//         return true;
//     }
    
//     public int GetBlock(int3 position)
//     {
//         if (state == ChunkGeneratorStates.GeneratingMap) return 0;
//         //if (!CanEditChunk()) return -1;
//         return blocksId[GetIndex(position)];
//     }

//     #endregion
    
//     #region Public

//     public ChunkData Save()
//     {
//         ChunkData data = new ChunkData(chunk);
//         return data;
//     }

//     public void Load(ChunkData data)
//     {
//         blocksId = new NativeArray<int>(data.BlockIds, Allocator.Persistent);
//         ChunkCoords = new int3(data.Coords[0], data.Coords[1], data.Coords[2]);
//     }
    
//     public void ForceUpdate() => state = ChunkGeneratorStates.RequiringUpdate;
    
//     public NativeArray<int> GetMeshBlocks() => blocksForMeshGeneration;

//     public NativeArray<int> GetBlocks()
//     {
//         //if (updating) throw new Exception("Cant get blocks");
//         return blocksId;
//     }

//     public void CreateStructure(int3 lPosition, int structureId)
//     {
//         //if (!CanEditChunk()) return;

//         int3 sSize = world.Structures[structureId].Size3;

//         for (int x = math.max(lPosition.x, 0); x < math.min(sSize.x + lPosition.x, Size.x); x++)
//         {
//             for (int y = math.max(lPosition.y, 0); y < math.min(sSize.y + lPosition.y, Size.y); y++)
//             {
//                 for (int z = math.max(lPosition.z, 0); z < math.min(sSize.z + lPosition.z, Size.z); z++)
//                 {
//                     int3 sBlockPos = new int3(x, y, z) - lPosition;
//                     int blockId = blocksId[GetIndex(new int3(x, y, z))];
//                     int structureBlockId = world.Structures[structureId].Blocks[sBlockPos.x][sBlockPos.y][sBlockPos.z];
//                     if (blockTypes.areReplacable[blockId] ||
//                         !blockTypes.areReplacable[structureBlockId])
//                     {
//                         blocksId[GetIndex(new int3(x, y, z))] = structureBlockId;
//                     }
//                 }
//             }
//         }
//         ForceUpdate();
//     }
    
//     #endregion

//     #region Private

//     // private void GetNeighboursData()
//     // {
//     //     ChunkNeighbourData = new ChunkNeighbourData();
//     //     for (int i = 0; i < 6; i++)
//     //     {
//     //         if (neighbours == null || neighbours[i] == null || !neighbours[i].CanEditChunk())
//     //         {
//     //             ChunkNeighbourData[i] = new NativeArray<int>(0, Allocator.Persistent);
//     //             ChunkNeighbourData.ChunkNeighbourDataValid[i] = false;
//     //             continue;
//     //         }
//     //         neighbours[i].NeighbourDepecency(1);
//     //         ChunkNeighbourData[i] = neighbours[i].GetMeshBlocks();
//     //         ChunkNeighbourData.ChunkNeighbourDataValid[i] = true;
//     //     }
//     // }
    
//     // public void FreeNeighboursData()
//     // {
//     //     for (int i = 0; i < 6; i++)
//     //     {
//     //         if (neighbours == null || neighbours[i] == null) continue;
//     //         neighbours[i].NeighbourDepecency(-1);
//     //     }
//     //     ChunkNeighbourData.Destroy();
//     // }
    
//     public void SetNeighbours(ChunkNeighbours chunkNeighbours) => neighbours = chunkNeighbours;

    
//     public void Hide()
//     {
//         meshFilter.mesh.Clear();
//     }
    
//     public void NeighbourDepecency(int value) => usedByOtherChunks += value;

//     #endregion

//     #endregion

//     private void GenerateBlockIdWithJobs()
//     {
//         blocksId = new NativeArray<int>(Size.x * Size.y * Size.z, Allocator.Persistent);

//         GenerateBlockIdJob generateBlockIdJob = new GenerateBlockIdJob
//         {
//             blockIdDatas = blocksId,

//             biome = biome,

//             chunkSize = Size,
//             chunkPosition = chunk.ChunkPosition
//         };

//         generatingBlockIdJobHandle = generateBlockIdJob.Schedule(Size.x * Size.y * Size.z, 32);
//     }
// }
