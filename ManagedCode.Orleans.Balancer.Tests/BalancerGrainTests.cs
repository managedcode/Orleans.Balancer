using FluentAssertions;
using Orleans.Runtime;
using Xunit;

namespace ManagedCode.Orleans.Balancer.Tests;

public class BalancerGrainTests : BaseTests
{
    [Fact]
    public async Task ActivateSomeSilo_ShouldBeOneBalancerGrain()
    {
        // Arrange
        short siloCount = 5;

        // Act
        var cluster = await DeployTestCluster(siloCount);
        await Task.Delay(2000);
        var managementGrain = cluster.GrainFactory.GetGrain<IManagementGrain>(0);

        // Assert
        var balancerGrains = await GetBalancerGrainStatistics(managementGrain);
        balancerGrains.Count.Should().Be(1);
    }

    [Fact]
    public async Task ActivateBalancerGrain_ShouldNotBeDeactivated()
    {
        // Act
        var cluster = await DeployTestCluster();

        await Task.Delay(1000);
        var managementGrain = cluster.GrainFactory.GetGrain<IManagementGrain>(0);
        await managementGrain.ForceActivationCollection(TimeSpan.FromMilliseconds(1));

        // Assert
        var balancerGrains = await GetBalancerGrainStatistics(managementGrain);
        balancerGrains.Count.Should().Be(1);
    }


    //
    // [Fact]
    // public async Task StopSiloWithBalancerGrains_BalancerGrainsShouldBeActive()
    // {
    //     // Act
    //     var cluster = await DeployTestCluster(5);
    //
    //     await Task.Delay(1000);
    //     var managementGrain = cluster.GrainFactory.GetGrain<IManagementGrain>(0);
    //     await managementGrain.ForceActivationCollection(TimeSpan.FromMilliseconds(1));
    //
    //     var silos = cluster.GetActiveSilos();
    //     var balancerGrain = (await GetBalancerGrainStatistics(managementGrain)).First();
    //
    //     await cluster.StopPrimarySiloAsync();   
    //     // await cluster.StopSiloAsync(silos.FirstOrDefault(s => s.SiloAddress == balancerGrain.SiloAddress));
    //
    //
    //     await Task.Delay(5000);
    //     managementGrain = cluster.GrainFactory.GetGrain<IManagementGrain>(1);
    //     var detailedGrainStatistics = await GetBalancerGrainStatistics(managementGrain);
    //
    //     // Assert
    //     var localDeactivatorGrains = detailedGrainStatistics
    //         .Where(s => s.GrainType.Contains(nameof(BalancerGrain)))
    //         .ToList();
    //
    //     localDeactivatorGrains.Count.Should().Be(1);
    // }
    //
    private static async Task<List<DetailedGrainStatistic>> GetBalancerGrainStatistics(IManagementGrain managementGrain)
    {
        var detailedGrainStatistics = await managementGrain.GetDetailedGrainStatistics();

        // Assert
        var balancerGrains = detailedGrainStatistics
            .Where(s => s.GrainType.Contains(nameof(BalancerGrain)))
            .ToList();

        return balancerGrains;
    }
}