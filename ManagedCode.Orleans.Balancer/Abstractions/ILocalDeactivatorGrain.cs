using Orleans.Concurrency;

namespace ManagedCode.Orleans.Balancer.Abstractions;

public interface ILocalDeactivatorGrain : IGrainWithStringKey
{
    [OneWay]
    Task InitializeAsync();

    [OneWay]
    Task DeactivateGrainsAsync(float percentage);
}