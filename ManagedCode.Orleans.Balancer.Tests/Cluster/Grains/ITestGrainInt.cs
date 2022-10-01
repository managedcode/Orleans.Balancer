using Orleans;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;

public interface ITestGrainInt : IGrainWithIntegerKey
{
    public Task<int> Do();
}