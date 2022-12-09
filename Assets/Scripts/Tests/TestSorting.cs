using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

public class TestSorting : MonoBehaviour
{
    ComputeBuffer buffer;
    ComputeBuffer test;
    ComputeBuffer test2;
    public ComputeShader shader;
    public int size = 24576;
    [ReadOnly] public int sizeP2 = 1;
    public int testSize = 1024;

    // Start is called before the first frame update
    void Start()
    {
        while (sizeP2 < size)
            sizeP2 *= 2;
        buffer = new ComputeBuffer(sizeP2, sizeof(int), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
        // test2 = new ComputeBuffer(size, sizeof(int));
        // test = new ComputeBuffer(testSize, sizeof(int));
        // data = new int[size];
        // SetData();
        // SetShader();
        T();
    }

    public int Correct = 0;
    public int Error = 0;

    void Update()
    {
        // SetShader();
        // if (!SetShader())
        // {
        //     Error++;
        //     Debug.Log("Error");
        // }
        // else
        // {
        //     Correct++;
        // // else Debug.Log("Correct"); 
        // }
    }
    public bool result = true;
    public int k = 0;

    void T()
    {
        int[] arr = new int[sizeP2];
        SetData();

        // Profiler.BeginSample("Dispatch");
        // shader.SetInt("ArrayLength", size);
        // shader.SetBuffer(0, "Array", buffer);
        // shader.Dispatch(0, (int)Mathf.Ceil(size / 1024f), 1, 1);
        // Profiler.EndSample();

        Profiler.BeginSample("Dispatch");
        shader.SetInt("ArrayLength", sizeP2);
        shader.SetBuffer(3, "Array", buffer);
        shader.Dispatch(3, 2, 1, 1);
        Profiler.EndSample();

        buffer.GetData(arr);
        PrintArray(arr);
    }

    bool SetShader()
    {
        for (int j = 0; j < testSize; j++)
        {
            SetData();
            
            for (int i = 0; i < 16; i++)
            // if (result)
            {
                Profiler.BeginSample("Dispatch");
                shader.SetInt("ArrayLength", size);
                shader.SetBuffer(0, "Array", buffer);
                shader.Dispatch(0, size / 1024, 1, 1);
                Profiler.EndSample();

            }
            shader.SetInt("testId", j);
            shader.SetBuffer(1, "Array", buffer);
            shader.SetBuffer(1, "tests", test);
            shader.Dispatch(1, size / 1024, 1, 1);
        }
        Profiler.BeginSample("GetData");
        int[] arr = new int[testSize];
        test.GetData(arr);
        result = !PrintArray(arr);// ? result : false;
        Profiler.EndSample();
        return result;
    }

    bool PrintArray(NativeArray<int> arr) => PrintArray(arr.ToArray());

    bool PrintArray(int[] arr)
    {
        string a = "";
        int b = 0;
        int pVal = 100;
        bool correct = true;
        for (int j = 0; j < arr.Length; j++)
        {
            // if (arr[j] == -1) break;
            // b++;
            if (pVal < arr[j]) 
            {
                correct = false;
                // break;
                a += "----";
            }
            if (arr[j] == 9)
                b++;
            pVal = arr[j];
            a += ", " + arr[j];
        }
        Debug.Log(a);
        Debug.Log(b);
        Debug.Log(correct);
        return correct;
    }

    // int[] data;
    void SetData()
    {
        var data = buffer.BeginWrite<int>(0, sizeP2);
        for (int i = 0; i < sizeP2; i++)
        {
            if (i < size)
                data[i] = Random.Range(0, 10);
            else
                data[i] = 0;
        }
        PrintArray(data);
        buffer.EndWrite<int>(sizeP2);
    }
}
