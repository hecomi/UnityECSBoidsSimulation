using UnityEngine;
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
    MeshInstanceRendererComponent rendererComponent;

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
            typeof(NeighborsEntityBuffer));
        var random = new Unity.Mathematics.Random(853);

        var renderer = rendererComponent.Value;
        Destroy(rendererComponent.gameObject);

        for (int i = 0; i < boidCount; ++i)
        {
            var entity = manager.CreateEntity(archetype);
            var rot = random.NextQuaternionRotation();
            manager.SetComponentData(entity, new Position { Value = math.normalize(random.NextFloat3(1f)) });
            manager.SetComponentData(entity, new Rotation { Value = rot });
            manager.SetComponentData(entity, new Scale { Value = new float3(boidScale.x, boidScale.y, boidScale.z) });
            manager.SetComponentData(entity, new Velocity { Value = math.mul(rot, new float3(0, 0, 1)) * param.initSpeed });
            manager.SetComponentData(entity, new Acceleration { Value = float3.zero });
            manager.AddSharedComponentData(entity, rendererComponent.Value);
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