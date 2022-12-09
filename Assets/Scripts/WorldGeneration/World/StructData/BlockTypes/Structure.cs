using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure", menuName = "CustomObjects/Structure")]
public class Structure : ScriptableObject
{
    public int3 Size;
    public List<int> Blocks;

    public int GetValue(int3 pos) => Blocks[GetIndex(pos)];

    private int GetIndex(int3 pos) => pos.x + (pos.y + pos.z * Size.y) * Size.x;
    // public void SetValue(int3 pos, int val) => Blocks[GetIndex(pos)] = val;

    // public void SetStructure(int3 size, int blockId)
    // {
    //     Size = size;
    //     Blocks = new List<int>();
    //     for (int x = 0; x < Size.x; x++)
    //     {
    //         for (int y = 0; y < Size.y; y++)
    //         {
    //             for (int z = 0; z < Size.z; z++)
    //             {
    //                 Blocks.Add(blockId);
    //             }
    //         }
    //     }
    // }

    // public void CreateTreeStructure()
    // {
    //     // Structure structure = Resources.Load<Structure>("Tree");
    //     Structure structure = this;
    //     structure.SetStructure(new int3(5, 7, 5), 0);

    //     for (int x = 0; x < 5; x++)
    //     {
    //         for (int y = 0; y < 2; y++)
    //         {
    //             for (int z = 0; z < 5; z++)
    //             {
    //                 structure.SetValue(new int3(x, 2 + y, z), 9);
    //             }
    //         }
    //     }

    //     structure.SetValue(new int3(2, 0, 2), 8);
    //     structure.SetValue(new int3(2, 1, 2), 8);
    //     structure.SetValue(new int3(2, 2, 2), 8);
    //     structure.SetValue(new int3(2, 3, 2), 8);
    //     structure.SetValue(new int3(2, 4, 2), 8);
    //     structure.SetValue(new int3(0, 2, 0), 0);
    //     structure.SetValue(new int3(0, 3, 0), 0);
    //     structure.SetValue(new int3(4, 2, 0), 0);
    //     structure.SetValue(new int3(4, 3, 0), 0);
    //     structure.SetValue(new int3(0, 2, 4), 0);
    //     structure.SetValue(new int3(0, 3, 4), 0);
    //     structure.SetValue(new int3(4, 2, 4), 0);
    //     structure.SetValue(new int3(4, 3, 4), 0);
    //     structure.SetValue(new int3(1, 4, 2), 9);
    //     structure.SetValue(new int3(3, 4, 2), 9);
    //     structure.SetValue(new int3(2, 4, 1), 9);
    //     structure.SetValue(new int3(2, 4, 3), 9);
    //     structure.SetValue(new int3(1, 5, 2), 9);
    //     structure.SetValue(new int3(3, 5, 2), 9);
    //     structure.SetValue(new int3(2, 5, 1), 9);
    //     structure.SetValue(new int3(2, 5, 3), 9);
    //     structure.SetValue(new int3(2, 5, 2), 9);
    // }
}
