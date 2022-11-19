using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DrawMeshInstancedIndirectDemo : MonoBehaviour {
    public int chunkSize;
    [Range(0, 1)]
    public float AmbientOcclusionIntensity = 0.5f;
    public Color AmbientOcclusionColor;

    public Material material;
    public ComputeShader compute;
    public Transform pusher;
    public Transform Camera;
    public List<Texture2D> Textures;
    private ComputeBuffer argsBuffer;

    public ComputeShader culling;
    private ComputeBuffer voteBuffer;
    private ComputeBuffer scanBuffer;
    private ComputeBuffer groupSumArrayBuffer;
    private ComputeBuffer scannedGroupSumBuffer;
    private ComputeBuffer culledPositionsBuffer;
    public Texture2DArray Texture2DArray;

    private Mesh mesh;
    private Bounds bounds;

    private int numThreadGroups, numVoteThreadGroups, numGroupScanThreadGroups;

    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience funciton which returns the stride of the struct.
    private struct MeshProperties {
        // public Matrix4x4 mat;
        public Vector3 position;
        public int rotation;
        public Vector4 color;

        public static int Size() {
            return
                // sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 3 + 
                sizeof(int) +
                // sizeof(float) * 4;      // color;
                sizeof(int);               // color;
        }
    }

    int[,,] blocksId;
    ComputeBuffer blocksIdsBuffer;
    int[] blockIsTransparent;
    ComputeBuffer blocksIsTransparentBuffer;
    
    ComputeBuffer blocksSideDatas;
    

    private void Setup() {
        Mesh mesh = CreateQuad();
        this.mesh = mesh;

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        bounds = new Bounds(transform.position, Vector3.one * chunkSize * 2);
        int size = chunkSize * chunkSize * chunkSize * 6;

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

        Debug.Log(numThreadGroups);

        
        CreateBlocks();
        CreateArgs(size);
        InitializeBuffers(size);
        CreateCompute(size);
        CreateTextureArray();
        CreateBlockBuffers();
    }

    void CreateTextureArray()
    {
        if (Textures.Count <= 0) return;
        Texture2D text = Textures[0];
        Texture2DArray = new Texture2DArray(text.width, text.height, Textures.Count, text.format, false);
        Texture2DArray.filterMode = FilterMode.Point;
        for (int i = 0; i < Textures.Count; i++)
        {
            Texture2DArray.SetPixels(Textures[i].GetPixels(), i);
        }
        Texture2DArray.Apply();
    }

    void CreateBlockBuffers()
    {
        blockIsTransparent = new int[Textures.Count];

        blockIsTransparent[0] = 1;

        blocksIsTransparentBuffer = new ComputeBuffer(Textures.Count, sizeof(int));
        blocksIsTransparentBuffer.SetData(blockIsTransparent);
    }

    private void InitializeBuffers(int size)
    {
        blocksIdsBuffer = new ComputeBuffer(blocksId.Length, sizeof(int));
        blocksIdsBuffer.SetData(blocksId);
        
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

    void CreateCompute(int size)
    {
        compute.SetInts("_ChunkSize", new int[] { chunkSize, chunkSize, chunkSize });
        compute.SetBuffer(0, "_BlockIds", blocksIdsBuffer);
        compute.SetBuffer(0, "_BlockSideDatas", blocksSideDatas);
    }

    // string S(uint[] array)
    // {
    //     string r = "";
    //     for (int i = 0; i < array.Length; i++)
    //     {
    //         r += array[i] + ", ";
    //     }
    //     return r;
    // }

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
        culling.SetVector("_CameraPosition", Camera.position);
        culling.SetInts("_ChunkSize", new int[] { chunkSize, chunkSize, chunkSize });
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
        Vector3 pos = Camera.position;
        compute.SetFloats("_CameraPosition", new float[] { pos.x, pos.y, pos.z });
        compute.Dispatch(0, chunkSize / 8, chunkSize / 8, chunkSize / 8);
    }

    

    void CreateBlocks()
    {
        blocksId = new int[chunkSize, chunkSize, chunkSize];
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (x == 0 || y == 0)
                        blocksId[x, y, z] = 0;
                    else if (y > x + z || 16 - y > x + z)
                        blocksId[x, y, z] = 0;
                    else if (y == x + z)
                        blocksId[x, y, z] = 2;
                    else
                        blocksId[x, y, z] = 1;
                }
            }
        }
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

    private void Start() {
        Setup();
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

    private void Update() {
        SetMaterialBuffers();

        RunCompute();
        Culling();
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void OnDisable()
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
}
