using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Boid.PureECS.Sample2
{

public class NeighborDetectionSystem : ComponentSystem
{
	struct Data
	{
		public readonly int Length;
		[ReadOnly] public ComponentDataArray<Position> positions;
		[ReadOnly] public EntityArray entities;
		public ComponentDataArray<Velocity> velocities;
		[WriteOnly] public BufferArray<NeighborsEntityBuffer> neighbors;
	}

	[Inject] Data data;

	protected override void OnUpdate()
	{
		var param = Bootstrap.Param;
        float prodThresh = math.cos(math.radians(param.neighborFov));
        float distThresh = param.neighborDistance;

		for (int i = 0; i < data.Length; ++i)
		{
			data.neighbors[i].Clear();

			float3 pos0 = data.positions[i].Value;
			float3 fwd0 = math.normalize(data.velocities[i].Value);

			for (int j = 0; j < data.Length; ++j)
			{
				if (i == j) continue;

				float3 pos1 = data.positions[j].Value;
				var to = pos1 - pos0;
				var dist = math.length(to);

				if (dist < distThresh)
				{
					var dir = math.normalize(to);
					var prod = Vector3.Dot(dir, fwd0);
					if (prod > prodThresh)
					{
						data.neighbors[i].Add(new NeighborsEntityBuffer() { Value = data.entities[j] });
					}
				}
			}
		}
	}
}

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
			data.accelerations[i] = new Acceleration() { Value = accel };
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

public class SeparationSystem : ComponentSystem
{
	struct Data
	{
		public readonly int Length;
		[ReadOnly] public ComponentDataArray<Position> positions;
		public ComponentDataArray<Acceleration> accelerations;
		[ReadOnly] public BufferArray<NeighborsEntityBuffer> neighbors;
	}

	[Inject] Data data;

	protected override void OnUpdate()
	{
		var param = Bootstrap.Param;

		for (int i = 0; i < data.Length; ++i)
		{
			var neighbors = data.neighbors[i].Reinterpret<Entity>();
			if (neighbors.Length == 0) continue;

			var force = float3.zero;
			var pos0 = data.positions[i].Value;
			var accel = data.accelerations[i].Value;

			for (int j = 0; j < neighbors.Length; ++j)
			{
				var pos1 = EntityManager.GetComponentData<Position>(neighbors[j]).Value;
				force += math.normalize(pos0 - pos1);
			}

			force /= neighbors.Length;
			var dAccel = force * param.separationWeight;
			data.accelerations[i] = new Acceleration() { Value = accel + dAccel };
		}
	}
}

public class AlignmentSystem : ComponentSystem
{
	struct Data
	{
		public readonly int Length;
		[ReadOnly] public ComponentDataArray<Velocity> velocities;
		public ComponentDataArray<Acceleration> accelerations;
		[ReadOnly] public BufferArray<NeighborsEntityBuffer> neighbors;
	}

	[Inject] Data data;

	protected override void OnUpdate()
	{
		var param = Bootstrap.Param;

		for (int i = 0; i < data.Length; ++i)
		{
			var neighbors = data.neighbors[i].Reinterpret<Entity>();
			if (neighbors.Length == 0) continue;

			var averageVelocity = float3.zero;
			var velocity = data.velocities[i].Value;
			var accel = data.accelerations[i].Value;

			for (int j = 0; j < neighbors.Length; ++j)
			{
				averageVelocity += EntityManager.GetComponentData<Velocity>(neighbors[j]).Value;
			}

			averageVelocity /= neighbors.Length;
			var dAccel = (averageVelocity - velocity) * param.alignmentWeight;
			data.accelerations[i] = new Acceleration() { Value = accel + dAccel };
		}
	}
}

public class CohesionSystem : ComponentSystem
{
	struct Data
	{
		public readonly int Length;
		[ReadOnly] public ComponentDataArray<Position> positions;
		public ComponentDataArray<Acceleration> accelerations;
		[ReadOnly] public BufferArray<NeighborsEntityBuffer> neighbors;
	}

	[Inject] Data data;

	protected override void OnUpdate()
	{
		var param = Bootstrap.Param;

		for (int i = 0; i < data.Length; ++i)
		{
			var neighbors = data.neighbors[i].Reinterpret<Entity>();
			if (neighbors.Length == 0) continue;

			var averagePos = float3.zero;
			var pos = data.positions[i].Value;
			var accel = data.accelerations[i].Value;

			for (int j = 0; j < neighbors.Length; ++j)
			{
				averagePos += EntityManager.GetComponentData<Position>(neighbors[j]).Value;
			}

			averagePos /= neighbors.Length;
			var dAccel = (averagePos - pos) * param.cohesionWeight;

			data.accelerations[i] = new Acceleration() { Value = accel + dAccel };
		}
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

			data.velocities[i] = new Velocity() { Value = velocity };
			data.positions[i] = new Position() { Value = pos };
			data.rotations[i] = new Rotation() { Value = rot };
			data.accelerations[i] = new Acceleration() { Value = float3.zero };
		}
	}
}

}