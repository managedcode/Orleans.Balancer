using FluentAssertions;
using ManagedCode.Orleans.Balancer.Tests.Cluster;
using Orleans.Runtime;
using Orleans.TestingHost;
using Xunit;

namespace ManagedCode.Orleans.Balancer.Tests;

public class LocalDeactivatorTests
{
    [Fact]
    public async Task ActivateSomeSilo_ShouldBeSameCountLocalDeactivators()
    {
        // Arrange
        short siloCount = 5;

        // Act
        var cluster = await DeployTestCluster(siloCount);
        await Task.Delay(2000);
        var managementGrain = cluster.GrainFactory.GetGrain<IManagementGrain>(0);
        var detailedGrainStatistics = await managementGrain.GetDetailedGrainStatistics();


        // Assert
        var localDeactivatorGrains = detailedGrainStatistics
            .Where(s => s.GrainType.Contains(nameof(LocalDeactivatorGrain)))
            .ToList();

        localDeactivatorGrains.Count.Should().Be(siloCount);

        foreach (var localDeactivatorGrain in localDeactivatorGrains)
        {
            var grainKey = localDeactivatorGrain.GrainId.Key.ToString();
            var siloAddress = localDeactivatorGrain.SiloAddress.ToParsableString();

            grainKey.Should().Be(siloAddress);
        }
    }

    [Fact]
    public async Task ActivateLocalDeactivators_ShouldNotBeDeactivated()
    {
        // Arrange
        short siloCount = 5;

        // Act
        var cluster = await DeployTestCluster(siloCount);
        await Task.Delay(2000);
        var managementGrain = cluster.GrainFactory.GetGrain<IManagementGrain>(0);
        await managementGrain.ForceActivationCollection(TimeSpan.FromMilliseconds(1));
        await Task.Delay(2000);

        var detailedGrainStatistics = await managementGrain.GetDetailedGrainStatistics();

        // Assert
        var localDeactivatorGrains = detailedGrainStatistics
            .Where(s => s.GrainType.Contains(nameof(LocalDeactivatorGrain)))
            .ToList();

        localDeactivatorGrains.Count.Should().Be(siloCount);
    }

    private static async Task<TestCluster> DeployTestCluster(short initialSilosCount)
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.Options.InitialSilosCount = initialSilosCount;
        var cluster = builder.Build();
        await cluster.DeployAsync();

        return cluster;
    }
}