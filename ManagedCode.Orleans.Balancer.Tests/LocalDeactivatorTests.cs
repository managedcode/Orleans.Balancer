using FluentAssertions;
using Orleans.Runtime;
using Xunit;

namespace ManagedCode.Orleans.Balancer.Tests;

public class LocalDeactivatorTests : BaseTests
{
    [Fact]
    public async Task ActivateSomeSilo_ShouldBeSameCountLocalDeactivators()
    {
        // Arrange
        short siloCount = 5;

        // Act
        var cluster = await DeployTestCluster(siloCount);
        await Task.Delay(1000);
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
        await Task.Delay(1000);
        var managementGrain = cluster.GrainFactory.GetGrain<IManagementGrain>(0);
        await managementGrain.ForceActivationCollection(TimeSpan.FromMilliseconds(1));

        //await Task.Delay(TimeSpan.FromMinutes(2.5));

        var detailedGrainStatistics = await managementGrain.GetDetailedGrainStatistics();

        // Assert
        var localDeactivatorGrains = detailedGrainStatistics
            .Where(s => s.GrainType.Contains(nameof(LocalDeactivatorGrain)))
            .ToList();

        localDeactivatorGrains.Count.Should().Be(siloCount);
    }
}