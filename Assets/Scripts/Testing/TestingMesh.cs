using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class TestingMesh : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CreateEntity();
    }

    private void CreateEntity()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld)
            );

        entityManager.CreateEntity(entityArchetype);
    }
}
