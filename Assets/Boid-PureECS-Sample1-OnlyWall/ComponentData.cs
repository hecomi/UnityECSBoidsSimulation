using Unity.Entities;
using Unity.Mathematics;

namespace Boid.PureECS.Sample1
{

public struct Velocity : IComponentData
{
    public float3 Value;
}

public struct Acceleration : IComponentData
{
    public float3 Value;
}

}