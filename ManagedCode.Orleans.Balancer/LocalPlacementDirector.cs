using Orleans.Runtime;
using Orleans.Runtime.Placement;

namespace ManagedCode.Orleans.Balancer;

internal sealed class LocalPlacementDirector : IPlacementDirector
{
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        return Task.FromResult(context.LocalSilo);
    }
}

[Serializable]
[GenerateSerializer]
internal class LocalPlacementStrategy : PlacementStrategy
{
}