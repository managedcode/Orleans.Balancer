using Orleans.Placement;

namespace ManagedCode.Orleans.Balancer.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal class LocalPlacementAttribute : PlacementAttribute
{
    public LocalPlacementAttribute()
        : base(new LocalPlacementStrategy())
    {
    }
}