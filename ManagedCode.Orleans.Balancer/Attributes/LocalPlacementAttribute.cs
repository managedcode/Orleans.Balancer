using Orleans.Placement;

namespace ManagedCode.Orleans.Balancer.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal class LocalPlacementAttribute : PlacementAttribute
{
    public LocalPlacementAttribute()
        : base(new LocalPlacementStrategy())
    {
    }
}