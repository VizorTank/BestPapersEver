using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class SaveManager
{
    public Dictionary<int3, IChunk> LoadedChunks;
    public Dictionary<int3, bool> GeneratedChunks;

    public void SaveChunk(IChunk chunk)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        ChunkData data = new ChunkData(chunk);
        string path = GetChunkName(chunk.GetChunkCoordinates());
        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, data);
        stream.Close();
        if (!GeneratedChunks.TryGetValue(chunk.GetChunkCoordinates(), out bool tmp))
            GeneratedChunks.Add(chunk.GetChunkCoordinates(), true);
    }

    public string GetChunkName(int3 chunkCoords)
    {
        return Application.persistentDataPath
                      + "/chunk"
                      + chunkCoords.x + "_"
                      + chunkCoords.y + "_"
                      + chunkCoords.z
                      + ".chunk";
    }

    public IChunk LoadChunk(int3 ChunkCoord, WorldClass World)
    {
        if (GeneratedChunks.TryGetValue(ChunkCoord, out bool tmp))
        {
            string path = GetChunkName(ChunkCoord);
            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path, FileMode.Open);
                ChunkData data = formatter.Deserialize(stream) as ChunkData;
                return new ChunkOld(World, data);
            }
        }
        GeneratedChunks.Add(ChunkCoord, true);
        return new ChunkOld(World, ChunkCoord);
    }
}
