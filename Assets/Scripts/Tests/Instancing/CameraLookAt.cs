using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookAt : MonoBehaviour
{
    public Transform block;

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(block);
    }
}
