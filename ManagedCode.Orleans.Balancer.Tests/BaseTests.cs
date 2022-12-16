using ManagedCode.Orleans.Balancer.Tests.Cluster;
using Orleans.TestingHost;

namespace ManagedCode.Orleans.Balancer.Tests;

public class BaseTests
{
    protected static async Task<TestCluster> DeployTestCluster(short initialSilosCount = 1)
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.Options.InitialSilosCount = initialSilosCount;
        var cluster = builder.Build();
        await cluster.DeployAsync();

        return cluster;
    }
}