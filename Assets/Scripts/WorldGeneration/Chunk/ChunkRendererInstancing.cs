using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ChunkRendererInstancing : IChunkRenderer
{
    private IWorld _world;
    private IChunk _chunk;

    private int3 chunkSize = VoxelData.ChunkSize;

    private float AmbientOcclusionIntensity = 0.5f;
    private Color AmbientOcclusionColor;

    private ComputeShader blocks;
    private ComputeBuffer argsBuffer;

    public ComputeShader culling;
    private ComputeBuffer voteBuffer;
    private ComputeBuffer scanBuffer;
    private ComputeBuffer groupSumArrayBuffer;
    private ComputeBuffer scannedGroupSumBuffer;
    private ComputeBuffer culledPositionsBuffer;
    private Texture2DArray Texture2DArray;

    ComputeBuffer blocksIdsBuffer;
    ComputeBuffer blocksIsTransparentBuffer;
    ComputeBuffer blocksSideDatas;

    private Mesh mesh;
    private Material material;
    private Bounds bounds;

    private int numThreadGroups;
    private int numVoteThreadGroups;
    private int numGroupScanThreadGroups;

    int[] blocksId;
    int[] blockIsTransparent;

    private struct MeshProperties {
        public Vector3 position;
        public int rotation;
        public Vector4 color;

        public static int Size() {
            return
                sizeof(float) * 3 + 
                sizeof(int) +
                sizeof(int);
        }
    }

    public ChunkRendererInstancing(IChunk chunk, IWorld world)
    {
        _world = world;
        _chunk = chunk;
    }

    private int size;

    private void Setup() {
        Mesh mesh = CreateQuad();
        this.mesh = mesh;

        material = new Material(_world.GetBlockTypesList().Material);
        blocks = _world.GetBlockTypesList().Blocks;
        culling = _world.GetBlockTypesList().Culling;

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        bounds = new Bounds(_chunk.GetChunkPosition() + new float3(0.5f, 0.5f, 0.5f), Vector3.one * chunkSize.x * 2);
        size = GetChunkVolume() * 3;

        numThreadGroups = Mathf.CeilToInt(size / 128.0f);
        if (numThreadGroups > 128) {
            int powerOfTwo = 128;
            while (powerOfTwo < numThreadGroups)
                powerOfTwo *= 2;
            
            numThreadGroups = powerOfTwo;
        } else {
            while (128 % numThreadGroups != 0)
                numThreadGroups++;
        }
        numVoteThreadGroups = Mathf.CeilToInt(size / 128.0f);
        numGroupScanThreadGroups = Mathf.CeilToInt(size / 1024.0f);
        
        // CreateBlocks();
        CreateArgs(size);
        InitializeBuffers(size);
        CreateTextureArray();
        CreateBlockBuffers();
    }

    int GetChunkVolume()
    {
        return chunkSize.x * chunkSize.y * chunkSize.z;
    }

    void CreateTextureArray()
    {
        Texture2DArray = _world.GetBlockTypesList().TextureArray;
        // if (Textures.Count <= 0) return;
        // Texture2D text = Textures[0];
        // Texture2DArray = new Texture2DArray(text.width, text.height, Textures.Count, text.format, false);
        // Texture2DArray.filterMode = FilterMode.Point;
        // for (int i = 0; i < Textures.Count; i++)
        // {
        //     Texture2DArray.SetPixels(Textures[i].GetPixels(), i);
        // }
        // Texture2DArray.Apply();
        // AssetDatabase.CreateAsset(Texture2DArray, "Assets/Texture2DArray.png");
    }

    void CreateBlockBuffers()
    {
        blocksIsTransparentBuffer = _world.GetBlockTypesList().BlocksIsTransparentBuffer;
        // blockIsTransparent = _world.GetBlockTypesList().areTransparentInt;

        // blocksIsTransparentBuffer = new ComputeBuffer(blockIsTransparent.Length, sizeof(int));
        // blocksIsTransparentBuffer.SetData(blockIsTransparent);
    }

    private void InitializeBuffers(int size)
    {
        blocksIdsBuffer = new ComputeBuffer(GetChunkVolume(), sizeof(int));
        
        blocksSideDatas = new ComputeBuffer(size, MeshProperties.Size());
        culledPositionsBuffer = new ComputeBuffer(size, MeshProperties.Size());

        voteBuffer = new ComputeBuffer(size, 4);
        scanBuffer = new ComputeBuffer(size, 4);
        groupSumArrayBuffer = new ComputeBuffer(numThreadGroups, 4);
        scannedGroupSumBuffer = new ComputeBuffer(numThreadGroups, 4);
    }

    void CreateArgs(int size)
    {
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)size;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }

    void Culling()
    {
        culling.SetBuffer(4, "_ArgsBuffer", argsBuffer);
        culling.Dispatch(4, 1, 1, 1);

        // Vote
        // culling.SetMatrix("MATRIX_VP", VP);
        culling.SetBuffer(0, "_BlockIdsBuffer", blocksIdsBuffer);
        culling.SetBuffer(0, "_BlockIsTransparentBuffer", blocksIsTransparentBuffer);
        culling.SetBuffer(0, "_BlockSideDataBuffer", blocksSideDatas);
        culling.SetBuffer(0, "_VoteBuffer", voteBuffer);
        // culling.SetVector("_CameraPosition", _world.GetPlayerPosition());
        culling.SetInts("_ChunkSize", new int[] { chunkSize.x, chunkSize.y, chunkSize.z });
        culling.Dispatch(0, numVoteThreadGroups, 1, 1);

        // Scan Instances
        culling.SetBuffer(1, "_VoteBuffer", voteBuffer);
        culling.SetBuffer(1, "_ScanBuffer", scanBuffer);
        culling.SetBuffer(1, "_GroupSumArray", groupSumArrayBuffer);
        culling.Dispatch(1, numThreadGroups, 1, 1);

        // Scan Groups
        culling.SetInt("_NumOfGroups", numThreadGroups);
        culling.SetBuffer(2, "_GroupSumArrayIn", groupSumArrayBuffer);
        culling.SetBuffer(2, "_GroupSumArrayOut", scannedGroupSumBuffer);
        culling.Dispatch(2, numGroupScanThreadGroups, 1, 1);

        // Compact
        culling.SetBuffer(3, "_BlockSideDataBuffer", blocksSideDatas);
        culling.SetBuffer(3, "_VoteBuffer", voteBuffer);
        culling.SetBuffer(3, "_ScanBuffer", scanBuffer);
        culling.SetBuffer(3, "_ArgsBuffer", argsBuffer);
        culling.SetBuffer(3, "_CulledBlockSideDataOutputBuffer", culledPositionsBuffer);
        culling.SetBuffer(3, "_GroupSumArray", scannedGroupSumBuffer);
        culling.Dispatch(3, numThreadGroups, 1, 1);
    }

    void RunCompute()
    {
        Vector3 pos = _world.GetPlayerPosition();
        blocks.SetInts("_ChunkSize", new int[] { chunkSize.x, chunkSize.y, chunkSize.z });
        blocks.SetFloats("_CameraPosition", new float[] { pos.x, pos.y, pos.z });
        blocks.SetBuffer(0, "_BlockIds", blocksIdsBuffer);
        blocks.SetBuffer(0, "_BlockSideDatas", blocksSideDatas);
        blocks.Dispatch(0, chunkSize.x / 8, chunkSize.y / 8, chunkSize.z / 8);
    }

    // void CreateBlocks()
    // {
    //     blocksId = new int[GetChunkVolume()];
    //     for (int x = 0; x < chunkSize.x; x++)
    //     {
    //         for (int y = 0; y < chunkSize.y; y++)
    //         {
    //             for (int z = 0; z < chunkSize.z; z++)
    //             {
    //                 int index = VoxelData.GetIndex(new int3(x, y, z));
    //                 if (x == 0 || y == 0 || z == 0)
    //                     blocksId[index] = 0;
    //                 else
    //                 {
    //                     if (y > x + z)
    //                         blocksId[index] = 0;
    //                     else
    //                         blocksId[index] = y % 10;
    //                 }
    //             }
    //         }
    //     }
    // }

    private Mesh CreateQuad(float width = 1f, float height = 1f) {
        // Create a quad mesh.
        var mesh = new Mesh();

        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[4] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0),
            new Vector3(w, h, 0)
        };

        var tris = new int[6] {
            // lower left tri.
            0, 2, 1,
            // lower right tri
            2, 3, 1
        };

        var normals = new Vector3[4] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    void SetMaterialBuffers()
    {
        material.SetBuffer("_BlockSideDataBuffer", culledPositionsBuffer);

        material.SetBuffer("_BlockIdBuffer", blocksIdsBuffer);
        material.SetBuffer("_BlockIsTransparentBuffer", blocksIsTransparentBuffer);

        material.SetTexture("_MainTexArray2", Texture2DArray);

        material.SetFloat("AmbientOcclusionIntensity", AmbientOcclusionIntensity);
        material.SetColor("AmbientOcclusionColor", AmbientOcclusionColor);
    }

    public void Destroy()
    {
        DisposeBuffer(ref blocksIdsBuffer);
        DisposeBuffer(ref blocksIsTransparentBuffer);
        DisposeBuffer(ref blocksSideDatas);
        DisposeBuffer(ref argsBuffer);
        DisposeBuffer(ref voteBuffer);
        DisposeBuffer(ref scanBuffer);
        DisposeBuffer(ref groupSumArrayBuffer);
        DisposeBuffer(ref scannedGroupSumBuffer);
        DisposeBuffer(ref culledPositionsBuffer);
    }

    private void DisposeBuffer(ref ComputeBuffer buffer)
    {
        if (buffer != null) {
            buffer.Release();
        }
        buffer = null;
    }

    public void Render(ChunkNeighbours neighbours)
    {
        // throw new System.NotImplementedException();
    }

    public void Render()
    {
        throw new System.NotImplementedException();
    }

    private bool _isCreated = false;

    private void Init()
    {
        Setup();
        _isCreated = true;
        Update();
    }

    public bool RequireProcessing()
    {
        if (!_isCreated) Init();

        // blocksId = _chunk.GetBlocks().ToArray();
        // Debug.Log(blocksId.Length);
        
        

        SetMaterialBuffers();

        RunCompute();
        CreateBlockBuffers();
        Culling();
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);

        return false;
    }

    public bool CanAccess()
    {
        return true;
    }

    public void Update()
    {
        if (!_isCreated) return;
        // blocksId = _chunk.GetBlocks().ToArray();
        Test();
        blocksIdsBuffer.SetData(blocksId);
        // throw new System.NotImplementedException();
    }

    private void Test()
    {
        blocksId = new int[GetChunkVolume()];
        blocksId = _chunk.GetBlocks().ToArray();
        // blocksId = _chunk.Temp();
        // int[] b = new int[GetChunkVolume()];
        // Array.Copy(_chunk.GetBlocks().ToArray(), blocksId, GetChunkVolume());
        // for (int x = 0; x < chunkSize.x; x++)
        // {
        //     for (int y = 0; y < chunkSize.y; y++)
        //     {
        //         for (int z = 0; z < chunkSize.z; z++)
        //         {
        //             int index = VoxelData.GetIndex(new int3(x, y, z));
        //             if (b[index] != 0)
        //                 blocksId[index] = (b[index] + 1) % 10;
        //         }
        //     }
        // }
    }

    public void Unload()
    {
        // throw new System.NotImplementedException();
    }
}
