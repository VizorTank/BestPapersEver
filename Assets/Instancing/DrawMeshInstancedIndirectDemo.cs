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
        // AssetDatabase.CreateAsset(Texture2DArray, "Assets/Texture2DArray.png");
    }

    void CreateBlockBuffers()
    {
        blockIsTransparent = new int[Textures.Count];

        blockIsTransparent[0] = 1;

        blocksIsTransparentBuffer = new ComputeBuffer(Textures.Count, sizeof(int));
        blocksIsTransparentBuffer.SetData(blockIsTransparent);
    }

    // bool a = false;

    // private Vector3[] p = new Vector3[] 
    // {
    //     new Vector3(0, 0, -1),
    //     new Vector3(0, 0, 1),
    //     new Vector3(0, 1, 0),
    //     new Vector3(0, -1, 0),
    //     new Vector3(-1, 0, 0),
    //     new Vector3(1, 0, 0)
    // };

    

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

        // material.SetTexture("_MainTexArray2", Texture2DArray);
        // material.SetBuffer("_BlockSideDataBuffer", culledPositionsBuffer);
    }

    // private void InitializeBuffers() {
    //     // int kernel = compute.FindKernel("CSMain");

    //     // Argument buffer used by DrawMeshInstancedIndirect.
    //     // uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    //     // Arguments for drawing mesh.
    //     // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        

    //     // Initialize buffer with the given population.
    //     // MeshProperties[] properties = new MeshProperties[population];
    //     List<MeshProperties> properties = new List<MeshProperties>();
    //     // int s = (int)Mathf.Pow(population, 1f / 3f);

    //     // for (int i = 0; i < population; i++) {
    //     //     if (i >= 6) break;
    //     //     if (i != 4 || i != 5) continue;
    //     //     // MeshProperties props = new MeshProperties();
    //     //     MeshProperties props = CreateSide(i);
    //     //     // // Vector3 position = new Vector3(Random.Range(-range, range), Random.Range(-range, range), Random.Range(-range, range));
    //     //     // // Vector3 position = new Vector3(i % s, (i / s) % s, (i / s / s) % s) + p[i];
    //     //     // Vector3 position = p[i] / 2;
    //     //     // // Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
    //     //     // Quaternion rotation = Quaternion.Euler(0, 0, 0);

    //     //     // if (i % 6 == 0) rotation = Quaternion.Euler(0, 0, 0);
    //     //     // if (i % 6 == 1) rotation = Quaternion.Euler(0, 180, 0);
    //     //     // if (i % 6 == 2) rotation = Quaternion.Euler(90, 0, 0);
    //     //     // if (i % 6 == 3) rotation = Quaternion.Euler(-90, 0, 0);
    //     //     // if (i % 6 == 4) rotation = Quaternion.Euler(0, 90, 0);
    //     //     // if (i % 6 == 5) rotation = Quaternion.Euler(0, -90, 0);
    //     //     // props.rotation = i % 6;
    //     //     // Vector3 scale = Vector3.one;

    //     //     // props.mat = Matrix4x4.TRS(position, rotation, scale);
    //     //     // props.color = Color.Lerp(Color.red, Color.blue, (float)i / 6);

    //     //     properties.Add(props);
    //     //     Debug.Log($"{props.mat.ToString()}");
            
    //     // }
    //     int size = chunkSize;
    //     for (int x = 0; x < size; x++)
    //     {
    //         for (int y = 0; y < size; y++)
    //         {
    //             for (int z = 0; z < size; z++)
    //             {
    //                 for (int i = 0; i < 6; i++)
    //                 {
    //                     properties.Add(CreateSide(i, new Vector3(x, y, z)));
    //                 }
    //             }
    //         }
    //     }

    //     // properties.Add(CreateSide(0));
    //     // properties.Add(CreateSide(1));
    //     // properties.Add(CreateSide(2));
    //     // properties.Add(CreateSide(3));
    //     // properties.Add(CreateSide(4));
    //     // properties.Add(CreateSide(5));
        
    //     // properties.Add(CreateSide(0, new Vector3(0, 2, 0)));
    //     // properties.Add(CreateSide(1, new Vector3(0, 2, 0)));
    //     // properties.Add(CreateSide(2, new Vector3(0, 2, 0)));
    //     // properties.Add(CreateSide(3, new Vector3(0, 2, 0)));
    //     // properties.Add(CreateSide(4, new Vector3(0, 2, 0)));
    //     // properties.Add(CreateSide(5, new Vector3(0, 2, 0)));

    //     Debug.Log(properties.Count);

    //     uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    //     args[0] = (uint)mesh.GetIndexCount(0);
    //     args[1] = (uint)properties.Count;
    //     args[2] = (uint)mesh.GetIndexStart(0);
    //     args[3] = (uint)mesh.GetBaseVertex(0);
    //     argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
    //     argsBuffer.SetData(args);

    //     meshPropertiesBuffer = new ComputeBuffer(properties.Count, MeshProperties.Size());
    //     meshPropertiesBuffer.SetData(properties);
    //     // compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);
    //     material.SetBuffer("_Properties", meshPropertiesBuffer);
    // }

    void CreateArgs(int size)
    {
        // int size = population * population * population;

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
        // int size = population * population * population;

        compute.SetInts("_ChunkSize", new int[] { chunkSize, chunkSize, chunkSize });
        compute.SetBuffer(0, "_BlockIds", blocksIdsBuffer);
        compute.SetBuffer(0, "_BlockSideDatas", blocksSideDatas);

        // material.SetBuffer("_Properties", blocksSideDatas);
        // RunCompute();
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
                    if (x == 0 || y == 0 || z == 0)
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

        // blocksId[1, 1, 1] = 1;

        // blocksId[4, 1, 1] = 1;
        // blocksId[4, 1, 2] = 1;

        // blocksId[7, 1, 1] = 1;
        // blocksId[8, 1, 2] = 1;

        // blocksId[10, 1, 1] = 1;
        // blocksId[11, 1, 2] = 1;
        // blocksId[11, 2, 2] = 1;

        // blocksId[13, 1, 1] = 1;
        // blocksId[13, 2, 1] = 1;
        // blocksId[14, 1, 2] = 1;
        // blocksId[14, 2, 2] = 1;
        // blocksId[14, 0, 2] = 1;
        // blocksId[13, 0, 2] = 1;
        // blocksId[14, 2, 1] = 1;
        // blocksId[13, 1, 3] = 1;
        // blocksId[14, 1, 3] = 1;
        // blocksId[14, 0, 3] = 1;

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

    private void OnDisable() {
        // if (meshPropertiesBuffer != null) {
        //     meshPropertiesBuffer.Release();
        // }
        // meshPropertiesBuffer = null;

        // if (argsBuffer != null) {
        //     argsBuffer.Release();
        // }
        // argsBuffer = null;
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
