using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingShaders : MonoBehaviour
{
    public ComputeShader shader;
    public RenderTexture renderTexture;

    void Start()
    {
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        shader.SetTexture(0, "Result", renderTexture);
        shader.Dispatch(0, renderTexture.width / 16, renderTexture.height / 16, 1);
    }
}
