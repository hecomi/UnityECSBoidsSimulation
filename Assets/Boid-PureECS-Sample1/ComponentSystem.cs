using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Boid.PureECS.Sample1
{

public class WallSystem : ComponentSystem
{
    struct Data
    {
        public readonly int Length;
        [ReadOnly] public ComponentDataArray<Position> positions;
        public ComponentDataArray<Acceleration> accelerations;
    }

    [Inject] Data data;

    protected override void OnUpdate()
    {
        var param = Bootstrap.Param;
        var scale = param.wallScale * 0.5f;
        var thresh = param.wallDistance;
        var weight = param.wallWeight;

        var r = new float3(+1, 0, 0);
        var u = new float3(0, +1, 0);
        var f = new float3(0, 0, +1);
        var l = new float3(-1, 0, 0);
        var d = new float3(0, -1, 0);
        var b = new float3(0, 0, -1);

        for (int i = 0; i < data.Length; ++i)
        {
            float3 pos = data.positions[i].Value;
            float3 accel = data.accelerations[i].Value;
            accel +=
                GetAccelAgainstWall(-scale - pos.x, r, thresh, weight) +
                GetAccelAgainstWall(-scale - pos.y, u, thresh, weight) +
                GetAccelAgainstWall(-scale - pos.z, f, thresh, weight) +
                GetAccelAgainstWall(+scale - pos.x, l, thresh, weight) +
                GetAccelAgainstWall(+scale - pos.y, d, thresh, weight) +
                GetAccelAgainstWall(+scale - pos.z, b, thresh, weight);
            data.accelerations[i] = new Acceleration { Value = accel };
        }
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

public class MoveSystem : ComponentSystem
{
    struct Data
    {
        public readonly int Length;
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<Rotation> rotations;
        public ComponentDataArray<Velocity> velocities;
        public ComponentDataArray<Acceleration> accelerations;
    }

    [Inject] Data data;

    protected override void OnUpdate()
    {
        var dt = Time.deltaTime;
        var minSpeed = Bootstrap.Param.minSpeed;
        var maxSpeed = Bootstrap.Param.maxSpeed;

        for (int i = 0; i < data.Length; ++i)
        {
            var velocity = data.velocities[i].Value;
            var pos = data.positions[i].Value;
            var rot = data.rotations[i].Value;
            var accel = data.accelerations[i].Value;

            velocity += accel * dt;
            var dir = math.normalize(velocity);
            var speed = math.length(velocity);
            velocity = math.clamp(speed, minSpeed, maxSpeed) * dir;
            pos += velocity * dt;
            rot = quaternion.LookRotationSafe(dir, new float3(0, 1, 0));

            data.velocities[i] = new Velocity { Value = velocity };
            data.positions[i] = new Position { Value = pos };
            data.rotations[i] = new Rotation { Value = rot };
            data.accelerations[i] = new Acceleration { Value = float3.zero };
        }
    }
}

}