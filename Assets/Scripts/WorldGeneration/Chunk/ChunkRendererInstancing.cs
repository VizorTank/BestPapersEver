using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class ChunkRendererInstancing : IChunkRenderer
{
    private IWorld _world;
    private IChunk _chunk;
    private int3 chunkSize = VoxelData.ChunkSize;

    private bool _isCreated = false;
    private bool _updated = true;
    private int size;


    // Compute Shaders
    private ComputeShader blocks;
    private ComputeShader _sorting;
    public ComputeShader culling;

    // Compute Buffers
    private ComputeBuffer blocksIdsBuffer;
    // Const
    private ComputeBuffer blocksIsTransparentBuffer;
    // Culling
    private ComputeBuffer voteBuffer;
    private ComputeBuffer scanBuffer;
    private ComputeBuffer groupSumArrayBuffer;
    private ComputeBuffer scannedGroupSumBuffer;
    private ComputeBuffer blocksSideDatas;
    // Result
    private ComputeBuffer culledPositionsBuffer;
    private ComputeBuffer sidesCountBuffer;

    // private Texture2DArray Texture2DArray;

    private List<Material> Materials;
    private List<ComputeBuffer> ShiftsBuffers;
    private List<ComputeBuffer> ArgsBuffers;

    private Mesh mesh;
    private Bounds bounds;

    private int numThreadGroups;
    private int numVoteThreadGroups;
    private int numGroupScanThreadGroups;

    private struct MeshProperties {
        public Vector3 position;
        public int rotation;
        public int type;

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

    private void Init()
    {
        Setup();
        _isCreated = true;
        _chunk.UpdateNeighbours();
        Update();
    }

    private void Setup() {
        Mesh mesh = CreateQuad();
        this.mesh = mesh;

        blocks = _world.GetBlockTypesList().Blocks;
        _sorting = _world.GetBlockTypesList().Sorting;
        culling = _world.GetBlockTypesList().Culling;

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        bounds = new Bounds(_chunk.GetChunkPosition() + new float3(0.5f, 0.5f, 0.5f), Vector3.one * chunkSize.x * 2);
        size = GetChunkVolume() * 8;

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
        
        InitializeBuffers(size);
        InitializeMaterialBuffers();
        // SetMaterialBuffers();
    }

    int GetChunkVolume()
    {
        return chunkSize.x * chunkSize.y * chunkSize.z;
    }

    private void InitializeBuffers(int size)
    {
        Profiler.BeginSample("Creating Instancing Buffers");
        blocksIdsBuffer = new ComputeBuffer(GetChunkVolume(), sizeof(int), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
        
        blocksSideDatas = new ComputeBuffer(size, MeshProperties.Size());
        culledPositionsBuffer = new ComputeBuffer(size, MeshProperties.Size());

        voteBuffer = new ComputeBuffer(size, 4);
        scanBuffer = new ComputeBuffer(size, 4);
        groupSumArrayBuffer = new ComputeBuffer(numThreadGroups, 4);
        scannedGroupSumBuffer = new ComputeBuffer(numThreadGroups, 4);

        sidesCountBuffer = new ComputeBuffer(1, sizeof(int));

        blocksIsTransparentBuffer = _world.GetBlockTypesList().BlocksIsTransparentBuffer;
        Profiler.EndSample();
    }

    public ComputeBuffer GetBlocksBuffer()
    {
        if (!_isCreated) 
        {
            Init();
        }

        return blocksIdsBuffer;
    }

    void InitializeMaterialBuffers()
    {
        Materials = new List<Material>();
        ShiftsBuffers = new List<ComputeBuffer>();
        ArgsBuffers = new List<ComputeBuffer>();
        for (int i = 1; i < _world.GetBlockTypesList().blockTypes.Count; i++)
        {
            ShiftsBuffers.Add(new ComputeBuffer(1, sizeof(int)));
            ArgsBuffers.Add(CreateArgsBuffer());
            Materials.Add(new Material(_world.GetBlockTypesList().blockTypes[i].material));

            Materials[i - 1].SetBuffer("_BlockSideDataBuffer", culledPositionsBuffer);
            Materials[i - 1].SetBuffer("_ShiftData", ShiftsBuffers[i - 1]);
        }
    }

    ComputeBuffer CreateArgsBuffer()
    {
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)0;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        ComputeBuffer buffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        buffer.SetData(args);
        return buffer;
    }

    private void UpdateShaderData(ChunkNeighbours neighbours)
    {
        if (!_chunk.CanAccess()) return;
        Profiler.BeginSample("Updating Instancing");
        
        Profiler.BeginSample("CreateBlockSides");
        CreateBlockSides();
        Profiler.EndSample();

        blocksIsTransparentBuffer = _world.GetBlockTypesList().BlocksIsTransparentBuffer;

        Profiler.BeginSample("Culling");
        Culling(neighbours);
        Profiler.EndSample();

        Profiler.BeginSample("SortBlockSides");
        SortBlockSides();
        Profiler.EndSample();

        Profiler.EndSample();
    }

    private void CreateBlockSides()
    {
        Profiler.BeginSample("Setting data");
        blocksIdsBuffer.SetData(_chunk.GetBlocks());
        Profiler.EndSample();

        Vector3 pos = _world.GetPlayerPosition();
        blocks.SetInts("_ChunkSize", new int[] { chunkSize.x, chunkSize.y, chunkSize.z });
        blocks.SetFloats("_CameraPosition", new float[] { pos.x, pos.y, pos.z });
        blocks.SetBuffer(0, "_BlockIds", blocksIdsBuffer);
        blocks.SetBuffer(0, "_BlockSideDatas", blocksSideDatas);
        blocks.Dispatch(0, chunkSize.x / 8, chunkSize.y / 8, chunkSize.z / 8);
    }

    private void Culling(ChunkNeighbours neighbours)
    {
        var buffers = neighbours.GetBufferData();

        // Vote
        culling.SetBuffer(0, "_BlockIdsBufferBack",  buffers[0]);
        culling.SetBuffer(0, "_BlockIdsBufferFront", buffers[1]);
        culling.SetBuffer(0, "_BlockIdsBufferTop",   buffers[2]);
        culling.SetBuffer(0, "_BlockIdsBufferBot",   buffers[3]);
        culling.SetBuffer(0, "_BlockIdsBufferLeft",  buffers[4]);
        culling.SetBuffer(0, "_BlockIdsBufferRight", buffers[5]);

        culling.SetBuffer(0, "_BlockIdsBuffer", blocksIdsBuffer);
        culling.SetBuffer(0, "_BlockIsTransparentBuffer", blocksIsTransparentBuffer);
        culling.SetBuffer(0, "_BlockSideDataBuffer", blocksSideDatas);
        culling.SetBuffer(0, "_VoteBuffer", voteBuffer);
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
        culling.SetBuffer(3, "_Size", sidesCountBuffer);
        culling.SetBuffer(3, "_CulledBlockSideDataOutputBuffer", culledPositionsBuffer);
        culling.SetBuffer(3, "_GroupSumArray", scannedGroupSumBuffer);
        culling.Dispatch(3, numThreadGroups, 1, 1);
    }

    private void SortBlockSides()
    {
        // Sort
        _sorting.SetBuffer(0, "_CulledBlockSideDataOutputBuffer", culledPositionsBuffer);
        _sorting.Dispatch(0, 1, 1, 1);//numGroupScanThreadGroups
    }
    
    void UpdateMaterials()
    {
        for (int i = 0; i < Materials.Count; i++)
        {
            UpdateMaterial(i);
        }
    }

    void UpdateMaterial(int i)
    {
        culling.SetBuffer(4, "_ArgsBuffer", ArgsBuffers[i]);
        culling.Dispatch(4, 1, 1, 1);

        culling.SetInt("_lookedValue", i + 1);
        culling.SetBuffer(5, "_CulledBlockSideDataOutputBuffer", culledPositionsBuffer);
        culling.SetBuffer(5, "_SizeReadOnly", sidesCountBuffer);
        culling.SetBuffer(5, "_ShiftValue", ShiftsBuffers[i]);
        culling.SetBuffer(5, "_ArgsBuffer", ArgsBuffers[i]);
        culling.Dispatch(5, numGroupScanThreadGroups, 1, 1);
    }

    void SetMaterialBuffers()
    {
        for (int i = 0; i < Materials.Count; i++)
        {
            Materials[i].SetBuffer("_BlockSideDataBuffer", culledPositionsBuffer);
            Materials[i].SetBuffer("_ShiftData", ShiftsBuffers[i]);
        }
    }

    public bool RequireProcessing()
    {
        if (!_isCreated) Init();

        return true;
    }

    public void Render(ChunkNeighbours neighbours)
    {
        if (!_updated)
        {
            _updated = true;
            UpdateShaderData(neighbours);
            UpdateMaterials();
        }
        DrawInstanced();
    }

    void DrawInstanced()
    {
        SetMaterialBuffers();
        GL.Flush();
        for (int i = 0; i < Materials.Count; i++)
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, Materials[i], bounds, ArgsBuffers[i]);
        }
    }

    public void Update()
    {
        if (!_isCreated) return;
        _updated = false;
    }

    public void Destroy()
    {
        if (!_isCreated) return;
        Profiler.BeginSample("Destroying Instancing Buffers");
        DisposeBuffer(ref blocksIdsBuffer);
        DisposeBuffer(ref blocksSideDatas);
        DisposeBuffer(ref voteBuffer);
        DisposeBuffer(ref scanBuffer);
        DisposeBuffer(ref groupSumArrayBuffer);
        DisposeBuffer(ref scannedGroupSumBuffer);
        DisposeBuffer(ref culledPositionsBuffer);

        DisposeBuffer(ref sidesCountBuffer);
        for (int i = 0; i < ShiftsBuffers.Count; i++)
        {
            ComputeBuffer s = ShiftsBuffers[i];
            DisposeBuffer(ref s);
            ComputeBuffer a = ArgsBuffers[i];
            DisposeBuffer(ref a);
        }
        Profiler.EndSample();
    }

    private void DisposeBuffer(ref ComputeBuffer buffer)
    {
        if (buffer != null) {
            buffer.Release();
        }
        buffer = null;
    }

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

}
