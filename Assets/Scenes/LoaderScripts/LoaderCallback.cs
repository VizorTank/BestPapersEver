using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoaderCallback : MonoBehaviour
{
    private bool ISFirstUpdate = true;

    private void Update()
    {
        if(ISFirstUpdate)
        {
            ISFirstUpdate = false;
            Loader.LoaderCallback();
        }
    }
}
