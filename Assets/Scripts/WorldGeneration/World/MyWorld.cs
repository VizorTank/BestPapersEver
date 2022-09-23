// using System.Collections.Generic;
// using Unity.Mathematics;
// using Unity.Collections;
// using UnityEngine;
// using System;

// public class MyWorld : MonoBehaviour, IWorld
// {

//     // public GameObject Player;

//     // public int WorldSizeInChunks = 64;
//     // public int WorldHeightInChunks = 16;
//     // public int RenderDistance = 4;
//     // //private IChunk[,,] _chunks;
//     // private Dictionary<int3, IChunk> _activeChunks = new Dictionary<int3, IChunk>();
//     // private Dictionary<int3, IChunk> _loadedChunks = new Dictionary<int3, IChunk>();
//     // private Dictionary<int3, IChunk> _chunksToUnload = new Dictionary<int3, IChunk>();

//     // private ISaveManager _saveManager;

//     // private void Start()
//     // {
//     //     _saveManager = MySaveManager.GetInstance();
//     //     //_chunks = new Chunk[WorldSizeInChunks, WorldHeightInChunks, WorldSizeInChunks];
//     // }

//     // private void Update()
//     // {
//     //     if (DidPlayerMoved())
//     //     {
//     //         LoadChunks(new int3(0, 0, 0));
//     //     }
//     //     DrawChunks();
//     // }

//     // public int3 playerLastChunkPos = new int3();

//     // private bool DidPlayerMoved()
//     // {
//     //     int3 playerChunkPos = VoxelData.GetChunkCoordinates(Player.transform.position);
//     //     playerChunkPos.y = 0;
//     //     int3 pPosDiff = playerChunkPos - playerLastChunkPos;
//     //     return math.any(pPosDiff != 0);
//     // }

//     // public void CreateNewChunk()
//     // {
//     //     throw new System.NotImplementedException();
//     // }

//     // public void DrawChunks()
//     // {
//     //     throw new System.NotImplementedException();
//     // }

//     // private int3[] _loadChunkGridSupportArrayDistance = new int3[] {
//     //     new int3(1, 0, 0),
//     //     new int3(0, 0, 1),
//     //     new int3(-1, 0, 0),
//     //     new int3(0, 0, -1)
//     // };

//     // private int3[] _loadChunkGridSupportArrayLength = new int3[] {
//     //     new int3(0, 0, 1),
//     //     new int3(1, 0, 0),
//     //     new int3(0, 0, -1),
//     //     new int3(-1, 0, 0)
//     // };

//     // // Loads new chunks when player moves
//     // private void LoadChunks(int3 centerPosition)
//     // {
//     //     _saveManager.UnloadChunks(this, _chunksToUnload);
//     //     _chunksToUnload = _activeChunks;
//     //     _activeChunks = new Dictionary<int3, IChunk>();
//     //     // for (int x = 0; x < RenderDistance; x++)
//     //     // {
//     //     //     for (int z = 0; z < RenderDistance; z++)
//     //     //     {
//     //     //         for (int y = 0; y < WorldHeightInChunks; y++)
//     //     //         {
//     //     //             LoadActiveChunk(new int3(x, y, z));
//     //     //         }
//     //     //     }
//     //     // }

//     //     for (int i = 1; i <= RenderDistance; i++)
//     //     {
//     //         for (int t = 0; t < 4; t++)
//     //         {
//     //             int3 chunkPos = _loadChunkGridSupportArrayDistance[t] * i;
//     //             for (int axis = -i + 1; axis <= i; axis++)
//     //             {
//     //                 for (int y = 0; y < WorldHeightInChunks; y++)
//     //                 {
//     //                     LoadActiveChunk(chunkPos + _loadChunkGridSupportArrayLength[t] * axis + new int3(0, y, 0));
//     //                 }
//     //             }
//     //         }
//     //     }
//     // }

//     // private void LoadActiveChunk(int3 chunkCoordinates)
//     // {
//     //     if (_chunksToUnload.ContainsKey(chunkCoordinates))
//     //     {
//     //         _activeChunks.Add(chunkCoordinates, _chunksToUnload[chunkCoordinates]);
//     //         _chunksToUnload.Remove(chunkCoordinates);
//     //         return;
//     //     }
//     //     else
//     //     {
//     //         IChunk chunk = _saveManager.LoadChunk(this, chunkCoordinates);
//     //         _activeChunks.Add(chunkCoordinates, chunk);
//     //     }

//     // }

//     public GameObject Player;
//     public WorldStaticData WorldData;

//     private int3 playerLastChunkPos = new int3();
//     private ISaveManager _saveManager;
//     private Dictionary<int3, IChunk> _activeChunks = new Dictionary<int3, IChunk>();
//     private Dictionary<int3, IChunk> _loadedChunks = new Dictionary<int3, IChunk>();
//     private Dictionary<int3, IChunk> _chunksToUnload = new Dictionary<int3, IChunk>();

//     public void Start()
//     {
//         _saveManager = SaveManager.GetInstance();
//     }

//     public void Update()
//     {
//         if (DidPlayerMoved())
//         {
//             LoadChunks(playerLastChunkPos);
//         }
//         DrawChunks();
//     }

//     private void DrawChunks()
//     {
//         foreach (var chunk in _activeChunks)
//         {
//             chunk.Value.Render();
//         }
//     }

//     private bool DidPlayerMoved()
//     {
//         int3 playerChunkPos = VoxelData.GetChunkCoordinates(Player.transform.position);
//         playerChunkPos.y = 0;
//         int3 pPosDiff = playerChunkPos - playerLastChunkPos;
//         playerLastChunkPos = playerChunkPos;
//         return math.any(pPosDiff != 0);
//     }

//     private int3[] _loadChunkGridSupportArrayDistance = new int3[] {
//         new int3(1, 0, 0),
//         new int3(0, 0, 1),
//         new int3(-1, 0, 0),
//         new int3(0, 0, -1)
//     };

//     private int3[] _loadChunkGridSupportArrayLength = new int3[] {
//         new int3(0, 0, 1),
//         new int3(1, 0, 0),
//         new int3(0, 0, -1),
//         new int3(-1, 0, 0)
//     };

//     private void LoadChunks(int3 centerPosition)
//     {
//         _saveManager.UnloadChunks(this, _chunksToUnload);
//         _chunksToUnload = _activeChunks;
//         _activeChunks = new Dictionary<int3, IChunk>();

//         for (int i = 1; i <= WorldData.RenderDistance; i++)
//         {
//             for (int t = 0; t < 4; t++)
//             {
//                 int3 chunkPos = _loadChunkGridSupportArrayDistance[t] * i;
//                 for (int axis = -i + 1; axis <= i; axis++)
//                 {
//                     for (int y = 0; y < WorldData.WorldSizeInChunks.y; y++)
//                     {
//                         LoadChunk(chunkPos + _loadChunkGridSupportArrayLength[t] * axis + new int3(0, y, 0));
//                     }
//                 }
//             }
//         }
//     }

//     private void LoadChunk(int3 chunkCoordinates)
//     {
//         if (_chunksToUnload.ContainsKey(chunkCoordinates))
//         {
//             _activeChunks.Add(chunkCoordinates, _chunksToUnload[chunkCoordinates]);
//             _chunksToUnload.Remove(chunkCoordinates);
//             return;
//         }
//         else
//         {
//             IChunk chunk = _saveManager.LoadChunk(this, chunkCoordinates);
//             _activeChunks.Add(chunkCoordinates, chunk);
//         }

//     }

//     public BiomeAttributesStruct GetBiome(int3 chunkCoordinates)
//     {
//         return WorldData.BiomeAttributes[0].GetBiomeStruct();
//     }

//     public BlockTypesList GetBlockTypesList()
//     {
//         return WorldData.BlockTypesList;
//     }

//     public ChunkNeighbours GetNeighbours(int3 chunkCoordinates)
//     {
//         throw new System.NotImplementedException();
//     }

//     public Transform GetTransform()
//     {
//         return transform;
//     }

//     public bool IsInWorld(int3 position)
//     {
//         throw new System.NotImplementedException();
//     }

//     public bool TryGetBlock(Vector3 position, ref int blockID)
//     {
//         throw new System.NotImplementedException();
//     }

//     public bool TryGetBlocks(Vector3 position, NativeArray<int> blockIds)
//     {
//         throw new System.NotImplementedException();
//     }

//     public bool TryPlaceBlock(Vector3 position, int blockID, ref int replacedBlockId)
//     {
//         throw new System.NotImplementedException();
//     }

//     public bool TrySetBlock(Vector3 position, int blockID, ref int replacedBlockId)
//     {
//         throw new System.NotImplementedException();
//     }

//     public bool TryGetNeighbours(int3 chunkCoordinates, ref ChunkNeighbours neighbours)
//     {
//         throw new NotImplementedException();
//     }

//     public IChunk GetChunk(int3 chunkCoordinates)
//     {
//         throw new NotImplementedException();
//     }

//     public Structure GetStructure(int structureId)
//     {
//         throw new NotImplementedException();
//     }
// }