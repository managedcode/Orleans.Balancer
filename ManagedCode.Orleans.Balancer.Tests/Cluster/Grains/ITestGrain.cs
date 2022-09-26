using Orleans;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;

public interface ITestGrain : IGrainWithGuidKey
{
    public Task<Guid> Do();
}

public interface ITestGrainInt : IGrainWithIntegerKey
{
    public Task<int> Do();
}