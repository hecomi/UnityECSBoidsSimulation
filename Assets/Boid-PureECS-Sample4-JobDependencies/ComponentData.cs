using Unity.Entities;
using Unity.Mathematics;

namespace Boid.PureECS.Sample4
{

public struct Velocity : IComponentData
{
    public float3 Value;
}

public struct Acceleration : IComponentData
{
    public float3 Value;
}

[InternalBufferCapacity(4)]
public unsafe struct NeighborsEntityBuffer : IBufferElementData
{
    public Entity Value;
}

}