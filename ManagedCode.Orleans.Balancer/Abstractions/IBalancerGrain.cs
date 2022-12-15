using Orleans.Concurrency;

namespace ManagedCode.Orleans.Balancer.Abstractions;

public interface IBalancerGrain : IGrainWithIntegerKey
{
    [OneWay]
    Task InitializeAsync();
}