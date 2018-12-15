using Unity.Entities;
using Unity.Mathematics;

namespace Boid.PureECS.Sample2
{

public struct Velocity : IComponentData
{
    public float3 Value;
}

public struct Acceleration : IComponentData
{
    public float3 Value;
}

[InternalBufferCapacity(8)]
public unsafe struct NeighborsEntityBuffer : IBufferElementData
{
    public Entity Value;
}

}