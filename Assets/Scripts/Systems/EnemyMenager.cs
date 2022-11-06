using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMenager
{
    static List<Object> enemies = new List<Object>();

    static EnemyMenager _instance;

    public static EnemyMenager GetInstance()
    {
        if (_instance == null)
        {
            _instance = new EnemyMenager();
        }
        return _instance;
    }


    private EnemyMenager()
    {
        enemies.Clear();
        UnityEngine.Object[] list = Resources.LoadAll("Enemies");
        foreach(Object enemy in list)
        {
            enemies.Add(enemy);
        }

    }

    public static void Destroy()
    {
        _instance = null;
    }

    public static Object GetEnemy(string EnemyName)
    {
        return enemies.Find(x => x.name == EnemyName);
    }



}
