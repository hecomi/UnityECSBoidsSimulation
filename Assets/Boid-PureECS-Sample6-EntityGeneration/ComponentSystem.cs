using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boid.PureECS.Sample6
{

[AlwaysUpdateSystem]
[UpdateBefore(typeof(BoidsSimulationSystem))]
public class BoidsEntityGenerationSystem : ComponentSystem
{
    EntityArchetype archetype;
    ComponentGroup group;
    MeshInstanceRenderer renderer;
    Unity.Mathematics.Random random;

    protected override void OnCreateManager()
    {
        if (!Bootstrap.IsValid) return;

        archetype = EntityManager.CreateArchetype(
            typeof(Position),
            typeof(Rotation),
            typeof(Scale),
            typeof(Velocity),
            typeof(Acceleration),
            typeof(NeighborsEntityBuffer),
            typeof(MeshInstanceRenderer));

        group = GetComponentGroup(archetype.ComponentTypes);

        random = new Unity.Mathematics.Random(853);

        renderer = new MeshInstanceRenderer 
        {
            castShadows = ShadowCastingMode.On,
            receiveShadows = true,
            mesh = Bootstrap.Boid.mesh,
            material = Bootstrap.Boid.material,
        };
    }

    protected override void OnUpdate()
    {
        if (!Bootstrap.IsValid) return;

        var entities = group.GetEntityArray();
        for (int i = 0; i < entities.Length - Bootstrap.Boid.count; ++i)
        {
            PostUpdateCommands.DestroyEntity(entities[i]);
        }
        for (int i = 0; i < Bootstrap.Boid.count - entities.Length; ++i)
        {
            CreateEntity();
        }
    }

    void CreateEntity()
    {
        var scale = Bootstrap.Boid.scale;
        var initSpeed = Bootstrap.Param.initSpeed;
        PostUpdateCommands.CreateEntity(archetype);
        PostUpdateCommands.SetComponent(new Position { Value = random.NextFloat3(1f) });
        PostUpdateCommands.SetComponent(new Rotation { Value = quaternion.identity });
        PostUpdateCommands.SetComponent(new Scale { Value = new float3(scale.x, scale.y, scale.z) });
        PostUpdateCommands.SetComponent(new Velocity { Value = random.NextFloat3Direction() * initSpeed });
        PostUpdateCommands.SetComponent(new Acceleration { Value = float3.zero });
        PostUpdateCommands.SetSharedComponent(renderer);
    }
}

public class BoidsSimulationSystem : JobComponentSystem
{
    ComponentGroup group;

    protected override void OnCreateManager()
    {
        group = GetComponentGroup(typeof(Position), typeof(Velocity), typeof(NeighborsEntityBuffer));
    }

    [BurstCompile]
    public struct NeighborsDetectionJob : IJobProcessComponentDataWithEntity<Position, Velocity>
    {
        [ReadOnly] public float prodThresh;
        [ReadOnly] public float distThresh;
        [ReadOnly] public ComponentDataFromEntity<Position> positionFromEntity;
        [ReadOnly] public BufferFromEntity<NeighborsEntityBuffer> neighborsFromEntity;
        [ReadOnly] public EntityArray entities;

        public void Execute(
            Entity entity,
            int index,
            [ReadOnly] ref Position pos,
            [ReadOnly] ref Velocity velocity)
        {
            neighborsFromEntity[entity].Clear();

            float3 pos0 = pos.Value;
            float3 fwd0 = math.normalize(velocity.Value);

            for (int i = 0; i < entities.Length; ++i)
            {
                var neighbor = entities[i];
                if (neighbor == entity) continue;

                float3 pos1 = positionFromEntity[neighbor].Value;
                var to = pos1 - pos0;
                var dist = math.length(to);

                if (dist < distThresh)
                {
                    var dir = math.normalize(to);
                    var prod = Vector3.Dot(dir, fwd0);
                    if (prod > prodThresh)
                    {
                        neighborsFromEntity[entity].Add(new NeighborsEntityBuffer { Value = neighbor });
                    }
                }
            }
        }
    }

    [BurstCompile]
    public struct WallJob : IJobProcessComponentData<Position, Acceleration>
    {
        [ReadOnly] public float scale;
        [ReadOnly] public float thresh;
        [ReadOnly] public float weight;

        public void Execute([ReadOnly] ref Position pos, ref Acceleration accel)
        {
            accel = new Acceleration
            {
                Value = accel.Value +
                    GetAccelAgainstWall(-scale - pos.Value.x, new float3(+1, 0, 0), thresh, weight) +
                    GetAccelAgainstWall(-scale - pos.Value.y, new float3(0, +1, 0), thresh, weight) +
                    GetAccelAgainstWall(-scale - pos.Value.z, new float3(0, 0, +1), thresh, weight) +
                    GetAccelAgainstWall(+scale - pos.Value.x, new float3(-1, 0, 0), thresh, weight) +
                    GetAccelAgainstWall(+scale - pos.Value.y, new float3(0, -1, 0), thresh, weight) +
                    GetAccelAgainstWall(+scale - pos.Value.z, new float3(0, 0, -1), thresh, weight)
            };
        }

        float3 GetAccelAgainstWall(float dist, float3 dir, float thresh, float weight)
        {
            if (dist < thresh)
            {
                return dir * (weight / math.abs(dist / thresh));
            }
            return float3.zero;
        }
    }

    [BurstCompile]
    public struct SeparationJob : IJobProcessComponentDataWithEntity<Position, Acceleration>
    {
        [ReadOnly] public float separationWeight;
        [ReadOnly] public BufferFromEntity<NeighborsEntityBuffer> neighborsFromEntity;
        [ReadOnly] public ComponentDataFromEntity<Position> positionFromEntity;

        public void Execute(Entity entity, int index, [ReadOnly] ref Position pos, ref Acceleration accel)
        {
            var neighbors = neighborsFromEntity[entity].Reinterpret<Entity>();
            if (neighbors.Length == 0) return;

            var pos0 = pos.Value;

            var force = float3.zero;
            for (int i = 0; i < neighbors.Length; ++i)
            {
                var pos1 = positionFromEntity[neighbors[i]].Value;
                force += math.normalize(pos0 - pos1);
            }
            force /= neighbors.Length;

            var dAccel = force * separationWeight;
            accel = new Acceleration { Value = accel.Value + dAccel };
        }
    }

    [BurstCompile]
    public struct AlignmentJob : IJobProcessComponentDataWithEntity<Velocity, Acceleration>
    {
        [ReadOnly] public float alignmentWeight;
        [ReadOnly] public BufferFromEntity<NeighborsEntityBuffer> neighborsFromEntity;
        [ReadOnly] public ComponentDataFromEntity<Velocity> velocityFromEntity;

        public void Execute(Entity entity, int index, [ReadOnly] ref Velocity velocity, ref Acceleration accel)
        {
            var neighbors = neighborsFromEntity[entity].Reinterpret<Entity>();
            if (neighbors.Length == 0) return;

            var averageVelocity = float3.zero;
            for (int i = 0; i < neighbors.Length; ++i)
            {
                averageVelocity += velocityFromEntity[neighbors[i]].Value;
            }
            averageVelocity /= neighbors.Length;

            var dAccel = (averageVelocity - velocity.Value) * alignmentWeight;
            accel = new Acceleration { Value = accel.Value + dAccel };
        }
    }

    [BurstCompile]
    public struct CohesionJob : IJobProcessComponentDataWithEntity<Position, Acceleration>
    {
        [ReadOnly] public float cohesionWeight;
        [ReadOnly] public BufferFromEntity<NeighborsEntityBuffer> neighborsFromEntity;
        [ReadOnly] public ComponentDataFromEntity<Position> positionFromEntity;

        public void Execute(Entity entity, int index, [ReadOnly] ref Position pos, ref Acceleration accel)
        {
            var neighbors = neighborsFromEntity[entity].Reinterpret<Entity>();
            if (neighbors.Length == 0) return;

            var averagePos = float3.zero;
            for (int i = 0; i < neighbors.Length; ++i)
            {
                averagePos += positionFromEntity[neighbors[i]].Value;
            }
            averagePos /= neighbors.Length;

            var dAccel = (averagePos - pos.Value) * cohesionWeight;
            accel = new Acceleration { Value = accel.Value + dAccel };
        }
    }

    [BurstCompile]
    public struct MoveJob : IJobProcessComponentData<Position, Rotation, Velocity, Acceleration>
    {
        [ReadOnly] public float dt;
        [ReadOnly] public float minSpeed;
        [ReadOnly] public float maxSpeed;

        public void Execute(
            ref Position pos,
            [WriteOnly] ref Rotation rot,
            ref Velocity velocity,
            ref Acceleration accel)
        {
            var v = velocity.Value;
            v += accel.Value * dt;
            var dir = math.normalize(v);
            var speed = math.length(v);
            v = math.clamp(speed, minSpeed, maxSpeed) * dir;

            pos = new Position { Value = pos.Value + v * dt };
            rot = new Rotation { Value = quaternion.LookRotationSafe(dir, new float3(0, 1, 0)) };
            velocity = new Velocity { Value = v };
            accel = new Acceleration { Value = float3.zero };
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var neighbors = new NeighborsDetectionJob
        {
            prodThresh = math.cos(math.radians(Bootstrap.Param.neighborFov)),
            distThresh = Bootstrap.Param.neighborDistance,
            neighborsFromEntity = GetBufferFromEntity<NeighborsEntityBuffer>(false),
            positionFromEntity = GetComponentDataFromEntity<Position>(true),
            entities = group.GetEntityArray(),
        };

        var wall = new WallJob
        {
            scale = Bootstrap.Param.wallScale * 0.5f,
            thresh = Bootstrap.Param.wallDistance,
            weight = Bootstrap.Param.wallWeight,
        };

        var separation = new SeparationJob 
        {
            separationWeight = Bootstrap.Param.separationWeight,
            neighborsFromEntity = GetBufferFromEntity<NeighborsEntityBuffer>(true),
            positionFromEntity = GetComponentDataFromEntity<Position>(true),
        };

        var alignment = new AlignmentJob 
        {
            alignmentWeight = Bootstrap.Param.alignmentWeight,
            neighborsFromEntity = GetBufferFromEntity<NeighborsEntityBuffer>(true),
            velocityFromEntity = GetComponentDataFromEntity<Velocity>(true),
        };

        var cohesion = new CohesionJob 
        {
            cohesionWeight = Bootstrap.Param.cohesionWeight,
            neighborsFromEntity = GetBufferFromEntity<NeighborsEntityBuffer>(true),
            positionFromEntity = GetComponentDataFromEntity<Position>(true),
        };

        var move = new MoveJob
        {
            dt = Time.deltaTime,
            minSpeed = Bootstrap.Param.minSpeed,
            maxSpeed = Bootstrap.Param.maxSpeed,
        };

        inputDeps = neighbors.Schedule(this, inputDeps);
        inputDeps = wall.Schedule(this, inputDeps);
        inputDeps = separation.Schedule(this, inputDeps);
        inputDeps = alignment.Schedule(this, inputDeps);
        inputDeps = cohesion.Schedule(this, inputDeps);
        inputDeps = move.Schedule(this, inputDeps);
        return inputDeps;
    }
}

}