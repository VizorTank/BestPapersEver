using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyLogger
{
    public static void Display(string value) => Debug.Log(value);
    public static void DisplayError(string value) => Debug.LogError("Controlled Error: " + value);
    public static void DisplayWarning(string value) => Debug.LogWarning("Controlled Warning: " + value);
}
