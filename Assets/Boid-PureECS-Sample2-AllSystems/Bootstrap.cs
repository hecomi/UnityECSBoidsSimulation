using UnityEngine;
using UnityEngine.Rendering;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;

namespace Boid.PureECS.Sample2
{

public class Bootstrap : MonoBehaviour 
{
    public static Bootstrap Instance 
    { 
        get; 
        private set; 
    }

    public static Param Param
    {
        get { return Instance.param; }
    }

    [SerializeField]
    int boidCount = 100;

    [SerializeField]
    Vector3 boidScale = new Vector3(0.1f, 0.1f, 0.3f);

    [SerializeField]
    Param param;

    [SerializeField]
    Mesh mesh;

    [SerializeField]
    Material material;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var manager = World.Active.GetOrCreateManager<EntityManager>();
        var archetype = manager.CreateArchetype(
            typeof(Position),
            typeof(Rotation),
            typeof(Scale),
            typeof(Velocity),
            typeof(Acceleration),
            typeof(MeshInstanceRenderer));
        var renderer = new MeshInstanceRenderer {
            castShadows = ShadowCastingMode.On,
            receiveShadows = true,
            mesh = mesh,
            material = material
        };
        var random = new Unity.Mathematics.Random(853);

        for (int i = 0; i < boidCount; ++i)
        {
            var entity = manager.CreateEntity(archetype);
            manager.SetComponentData(entity, new Position { Value = random.NextFloat3(1f) });
            manager.SetComponentData(entity, new Rotation { Value = quaternion.identity });
            manager.SetComponentData(entity, new Scale { Value = new float3(boidScale.x, boidScale.y, boidScale.z) });
            manager.SetComponentData(entity, new Velocity { Value = random.NextFloat3Direction() * param.initSpeed });
            manager.SetComponentData(entity, new Acceleration { Value = float3.zero });
            manager.SetSharedComponentData(entity, renderer);
        }
    }

    void OnDrawGizmos()
    {
        if (!param) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * param.wallScale);
    }
}

}