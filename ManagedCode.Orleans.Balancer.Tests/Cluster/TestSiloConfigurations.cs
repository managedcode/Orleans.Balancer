using ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster;

public class TestSiloConfigurations : ISiloConfigurator {
    public void Configure(ISiloBuilder siloBuilder) {
        siloBuilder.ConfigureServices(services => {
            // services.AddSingleton<T, Impl>(...);
        });
        siloBuilder.ConfigureApplicationParts(parts => parts.AddFromAppDomain());
        siloBuilder.UseActivationShedding(o =>
        {
            
        });
    }
}