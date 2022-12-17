using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;

public interface IDoGrain
{
    public Task<GrainId> Do();
}