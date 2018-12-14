using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Boid.HybridECS
{

[Serializable]
public struct Velocity : IComponentData
{
	public float3 Value;
}

[UnityEngine.DisallowMultipleComponent]
public class VelocityComponent : ComponentDataWrapper<Velocity>
{
}

}