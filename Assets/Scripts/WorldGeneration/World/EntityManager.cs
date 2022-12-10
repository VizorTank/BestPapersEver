using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EntityManager
{
    private IWorld _world;
    private EnemyMenager _enemyManager;

    private List<GameObject> _entities = new List<GameObject>();

    private int3 minSpawnDistance = VoxelData.ChunkSize;

    public EntityManager(IWorld world)
    {
        _world = world;
    }

    public void Run()
    {
        SpawnAlgorytm();
        DespawnAlgorytm();
    }

    public void SpawnAlgorytm()
    {
        int i = 0;
        int goToEnemyCount = GetGoToEnemyCount();
        while (i++ < 20 && _entities.Count < goToEnemyCount)
        {
            TrySpawnEnemy();
        }
    }

    public void DespawnAlgorytm()
    {
        int renderDistance = _world.GetRenderDistance() * VoxelData.ChunkSize.x;
        int worldHeight = _world.GetWorldHeightInChunks() * VoxelData.ChunkSize.y;

        for (int i = 0; i < _entities.Count; i++)
        {
            if (_entities[i] == null)
            {
                _entities.RemoveAt(i--);
                continue;
            }

            float3 pos = _entities[i].transform.position;
            if (pos.x < -renderDistance || pos.x > renderDistance ||
                pos.y <               0 || pos.y > worldHeight ||
                pos.z < -renderDistance || pos.z > renderDistance)
            {
                DespawnEnemy(_entities[i]);
                _entities.RemoveAt(i--);
                continue;
            }
        }
    }

    public void DespawnEnemy(GameObject enemy)
    {
        MonoBehaviour.Destroy(enemy);
    }

    public void TrySpawnEnemy()
    {
        Vector3 pPos = _world.GetPlayerPosition();
        int3 positionCandidate = GetRandomInt3() + (int3)math.floor(new float3(pPos.x, 0, pPos.z));
        if (!CheckIfEnemyCanSpawn(positionCandidate)) return;

        int blockId = 0;
        while (true)
        {
            if (!_world.TryGetBlock(positionCandidate - new int3(0, 1, 0), ref blockId)) return;
            if (_world.GetBlockTypesList().areSolid[blockId]) break;
            positionCandidate -= new int3(0, 1, 0);
        }
        SpawnEnemyAtPosition(positionCandidate);
    }

    public void SpawnEnemyAtPosition(int3 position)
    {
        Object enemyPreset = EnemyMenager.GetEnemy(2);
        if (enemyPreset != null)
        {
            GameObject enemy = MonoBehaviour.Instantiate(enemyPreset) as GameObject;
            enemy.transform.position = (float3)position;
            _entities.Add(enemy);

            Debug.Log("Spawned " + enemyPreset.name);
        }
        else
        {
            Debug.Log("Didn't Find " + enemyPreset.name);
        }
    }

    public bool CheckIfEnemyCanSpawn(int3 position)
    {
        if (position.x < minSpawnDistance.x && position.x > -minSpawnDistance.x &&
            position.z < minSpawnDistance.z && position.z > -minSpawnDistance.z) 
            return false;

        int blockId = 0;
        if (!_world.TryGetBlock(position, ref blockId) || _world.GetBlockTypesList().areSolid[blockId]) return false;
        if (!_world.TryGetBlock(position + new int3(0, 1, 0), ref blockId) || _world.GetBlockTypesList().areSolid[blockId]) return false;

        return true;
    }

    public int3 GetRandomInt3()
    {
        float renderDistance = _world.GetRenderDistance();
        float3 result = new float3(
            GetRandomFloat(-renderDistance, renderDistance),
            GetRandomFloat(0, _world.GetWorldHeightInChunks()),
            GetRandomFloat(-renderDistance, renderDistance)
        );

        result *= VoxelData.ChunkSize;

        return (int3)math.floor(result);
    }

    public float GetRandomFloat(float min, float max)
    {
        return UnityEngine.Random.Range(min, max);
    }

    public int GetGoToEnemyCount()
    {
        return (int)(math.pow(_world.GetRenderDistance(), 2) * _world.GetEnemiesDensity());
    }
}