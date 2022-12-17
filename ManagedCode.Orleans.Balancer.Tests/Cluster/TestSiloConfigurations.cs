using ManagedCode.Orleans.Balancer.Attributes;
using ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;
using Orleans.TestingHost;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster;

public class TestSiloConfigurations : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.ConfigureServices(services =>
        {
            // services.AddSingleton<T, Impl>(...);
        });
        siloBuilder.UseOrleansBalancer(o =>
        {
            o.GrainsForDeactivation.Add(typeof(TestGrainInt), DeactivationPriority.High);
        });
    }
}