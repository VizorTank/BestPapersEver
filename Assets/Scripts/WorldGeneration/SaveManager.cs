using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Profiling;

public class SaveManager : ISaveManager
{
    // public Dictionary<int3, IChunk> LoadedChunks;

    #region Singleton
    // private static ISaveManager _instance;
    private static string _applicationPath;
    private SaveManager(IWorld world)
    { 
        _generatedChunks = new Dictionary<int3, IChunk>();
        _generatedChunks.Clear();

        _worldPath = _applicationPath + "/" + world.GetWorldName();
        if(!System.IO.Directory.Exists(_worldPath))
            System.IO.Directory.CreateDirectory(_worldPath);
        
        string chunksPath = _worldPath + "/Chunks";
        if(!System.IO.Directory.Exists(chunksPath))
            System.IO.Directory.CreateDirectory(chunksPath);
    }
    public static ISaveManager GetInstance(IWorld world)
    {
        // if (_instance == null)
        // {
        //     _instance = new SaveManager();
        //     _applicationPath = Application.persistentDataPath;
        // }
        _applicationPath = Application.persistentDataPath;
        
        // return _instance;
        return new SaveManager(world);
    }
    #endregion

    // private bool _shouldSave = true;
    // private bool _shouldLoad = true;

    private string _worldPath;

    private Dictionary<int3, IChunk> _generatedChunks = new Dictionary<int3, IChunk>();
    private List<Task<ChunkData>> _chunksToLoad = new List<Task<ChunkData>>();
    private Dictionary<int3, IChunk> _chunksToSave = new Dictionary<int3, IChunk>();

    public void Run()
    {
        LoadingChunks();
        SavingChunks();
    }

    public void SaveWorld()
    {
        SavingChunks();
        string path = _worldPath + "/world.world";

        BinaryFormatter formatter = new BinaryFormatter();
        WorldData data = new WorldData(_generatedChunks);

        Debug.Log("Saved " + path);

        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, data);
        stream.Close();
    }

    public void LoadWorld()
    {
        string path = _worldPath + "/world.world";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            WorldData data = formatter.Deserialize(stream) as WorldData;

            stream.Close();
            //Debug.Log("Chunk loaded!");
            _generatedChunks = data.GetGeneratedChunks();
        }
        else
        {
            Debug.Log("File does not exists!");
            _generatedChunks = new Dictionary<int3, IChunk>();
        }
    }

    

    public string GetChunkPath(int3 chunkCoords)
    {
        return _worldPath
                      + "/Chunks/chunk"
                      + "_" + chunkCoords.x 
                      + "_" + chunkCoords.y
                      + "_" + chunkCoords.z
                      + ".chunk";
    }

    public void SavingChunks()
    {
        // Dictionary<int3, IChunk> chunksToSave = new Dictionary<int3, IChunk>();

        foreach (var item in _chunksToSave)
        {
            if (item.Value == null) continue;
            if (SaveChunk(item.Value))
                item.Value.Destroy();
            // else
            // {
            //     chunksToSave.Add(item.Key, item.Value);
                
            // }
        }
        // _chunksToSave = chunksToSave;
    }

    // public void WaitForChunksToSave()
    // {
    //     Debug.Log("Waiting for all chunks to save");
    //     int i = 0;
    //     while (_chunksToSave.Count > 0)
    //     {
    //         SavingChunks();
    //         if (i++ > 1000) break;
    //     }
    //     Debug.Log("All Chunks saved");
    // }

    public bool SaveChunk(IChunk chunk)
    {
        if (!chunk.CanBeSaved())
        {
            // Debug.Log("Cant access chunk");
            return false;
        }

        int3 chunkCoordinates = chunk.GetChunkCoordinates();

        BinaryFormatter formatter = new BinaryFormatter();
        ChunkData data = new ChunkData(chunk);

        string path = GetChunkPath(chunkCoordinates);
        //Debug.Log("Saved " + path);

        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, data);
        stream.Close();

        if (!_generatedChunks.ContainsKey(chunkCoordinates))
            _generatedChunks.Add(chunkCoordinates, null);
        else
            _generatedChunks[chunkCoordinates] = null;
        
        return true;
    }

    public void LoadingChunks()
    {
        List<Task<ChunkData>> chunksToLoad = new List<Task<ChunkData>>();
        foreach (Task<ChunkData> task in _chunksToLoad)
        {
            if (this == null) return;
            if (!task.IsCompleted)
            {
                chunksToLoad.Add(task);
                continue;
            }
            
            ChunkData data = task.Result;
            int3 coords = new int3(data.Coords[0], data.Coords[1], data.Coords[2]);
            if (_generatedChunks.ContainsKey(coords))
                _generatedChunks[coords].Init(data);
        }
        _chunksToLoad = chunksToLoad;
    }

    public IChunk LoadChunk(IWorld world, int3 chunkCoord)
    {
        if (_generatedChunks.ContainsKey(chunkCoord))
        {
            if (_generatedChunks[chunkCoord] != null)
            {
                return _generatedChunks[chunkCoord];
            }
            else
            {
                _generatedChunks[chunkCoord] = new Chunk(world, chunkCoord);
                LoadChunkFromFile(chunkCoord);
                return _generatedChunks[chunkCoord];
            }
        }
        else
        {
            _generatedChunks.Add(chunkCoord, new Chunk(world, chunkCoord));
            _generatedChunks[chunkCoord].Init();
            return _generatedChunks[chunkCoord];
        }
    }

    public void UnloadChunks(IWorld world, Dictionary<int3, IChunk> chunksToUnload)
    {
        foreach (var chunk in chunksToUnload)
        {
            UnloadChunk(world, chunk.Value);
        }
    }

    public void UnloadChunk(IWorld world, IChunk chunk)
    {
        if (chunk == null || _chunksToSave.ContainsKey(chunk.GetChunkCoordinates())) return;

        _chunksToSave.Add(chunk.GetChunkCoordinates(), chunk);

        // SaveChunk(chunk);
        // // GeneratedChunks[chunk.GetChunkCoordinates()] = null;
        // chunk.Destroy();
    }

    

    private void LoadChunkFromFile(int3 chunkCoord)
    {
        Profiler.BeginSample("GetChunkPath");
        string path = GetChunkPath(chunkCoord);
        Profiler.EndSample();

        Profiler.BeginSample("CreateTask");
        Task<ChunkData> task = Task.Factory.StartNew<ChunkData>(() => {
            if (File.Exists(path))
            // if (false)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path, FileMode.Open);

                ChunkData data = formatter.Deserialize(stream) as ChunkData;

                stream.Close();
                //Debug.Log("Chunk loaded!");
                return data;
            }
            else
            {
                // Debug.LogWarning("File does not exists!");
                return new ChunkData(chunkCoord);
            }
        });
        Profiler.EndSample();

        _chunksToLoad.Add(task);
    }

    public static void Destroy()
    {
        // _instance = null;
    }
}
