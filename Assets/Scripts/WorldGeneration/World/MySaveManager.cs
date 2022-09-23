// using System.Collections.Generic;
// using Unity.Mathematics;

// using UnityEngine;
// using System.IO;
// using System.Runtime.Serialization.Formatters.Binary;

// public class MySaveManager : ISaveManager
// {
//     #region Singleton
//     private static MySaveManager _instance;
//     private MySaveManager() { }
//     public static ISaveManager GetInstance()
//     {
//         if (_instance == null)
//             _instance = new MySaveManager();
        
//         return _instance;
//     }
//     #endregion

//     private Dictionary<int3, IChunk> _createdChunks;

//     public IChunk LoadChunk(IWorld world, int3 chunkCoordinates)
//     {
//         if (_createdChunks.ContainsKey(chunkCoordinates))
//             _createdChunks.Add(chunkCoordinates, LoadChunkFormFile(world, chunkCoordinates));
//         return _createdChunks[chunkCoordinates];
//     }

//     private string GetChunkFileName(int3 chunkCoordinates)
//     {
//         return "chunk" 
//             + "_" + chunkCoordinates.x
//             + "_" + chunkCoordinates.y
//             + "_" + chunkCoordinates.z
//             + ".chunk";
//     }

//     private void SaveChunkToFile(IChunk chunk)
//     {
//         ChunkData data = new ChunkData(chunk);

//         BinaryFormatter formatter = new BinaryFormatter();

//         string path = Application.persistentDataPath 
//             + "/" + GetChunkFileName(chunk.GetChunkCoordinates());

//         Debug.Log(path);

//         FileStream stream = new FileStream(path, FileMode.Create);

//         formatter.Serialize(stream, data);

//         stream.Close();
//     }

//     private IChunk LoadChunkFormFile(IWorld world, int3 chunkCoordinates)
//     {
//         ChunkData data = LoadChunkDataFromFile(chunkCoordinates);
//         Chunk chunk = new Chunk(world, data);
//         return chunk;
//     }

//     private ChunkData LoadChunkDataFromFile(int3 chunkCoordinates)
//     {
//         string path = Application.persistentDataPath 
//             + "/" + GetChunkFileName(chunkCoordinates);

//         if (File.Exists(path))
//         {
//             BinaryFormatter formatter = new BinaryFormatter();
//             FileStream stream = new FileStream(path, FileMode.Open);

//             ChunkData data = formatter.Deserialize(stream) as ChunkData;

//             stream.Close();

//             return data;
//         }
//         else
//         {
//             Debug.LogError("Save File Not Found In " + path);
//             return null;
//         }
//     }

//     public void SetChunkActive(int3 chunkCoordinates)
//     {
//         throw new System.NotImplementedException();
//     }

//     public void RemoveUnusedChunks()
//     {
//         throw new System.NotImplementedException();
//     }

//     public void UnloadChunks(IWorld world, Dictionary<int3, IChunk> chunksToUnload)
//     {

//     }

//     public void UnloadChunk(IWorld world, IChunk chunk)
//     {
//         throw new System.NotImplementedException();
//     }

//     public void SaveWorld()
//     {
//         throw new System.NotImplementedException();
//     }

//     public void LoadWorld()
//     {
//         throw new System.NotImplementedException();
//     }
// }