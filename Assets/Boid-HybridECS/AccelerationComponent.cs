using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Boid.HybridECS
{

[Serializable]
public struct Acceleration : IComponentData
{
	public float3 Value;
}

[UnityEngine.DisallowMultipleComponent]
public class AccelerationComponent : ComponentDataWrapper<Acceleration>
{
}

}