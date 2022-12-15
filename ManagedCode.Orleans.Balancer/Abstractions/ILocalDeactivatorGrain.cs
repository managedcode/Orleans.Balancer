namespace ManagedCode.Orleans.Balancer.Abstractions;

public interface ILocalDeactivatorGrain : IGrainWithStringKey
{
    Task InitializeAsync();
}