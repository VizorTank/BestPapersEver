using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure", menuName = "CustomObjects/Structures")]
public class Structure : ScriptableObject
{
    public Vector3Int Size;
    public int3 Size3 { get => new int3(Size.x, Size.y, Size.z); }
    public Vector3Int Hitbox;
    public Vector3Int HitboxOffset;

    public int BlockId;
    public List<List<List<int>>> Blocks;

    public Structure(Vector3Int size) 
    {
        SetStructure(size, 0);
    }

    public Structure(Vector3Int size, int blockId)
    {
        SetStructure(size, blockId);
    }

    private void SetStructure(Vector3Int size, int blockId)
    {
        Size = size;
        Blocks = new List<List<List<int>>>();
        for (int x = 0; x < Size.x; x++)
        {
            Blocks.Add(new List<List<int>>());
            for (int y = 0; y < Size.y; y++)
            {
                Blocks[x].Add(new List<int>());
                for (int z = 0; z < Size.z; z++)
                {
                    Blocks[x][y].Add(blockId);
                }
            }
        }
    }
}
