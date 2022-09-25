using System.Diagnostics;
using FluentAssertions;
using ManagedCode.Orleans.Balancer.Tests.Cluster;
using ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;
using Orleans.Runtime;
using Orleans.TestingHost;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Orleans.Balancer.Tests;

public class SiloTests 
{
    private readonly ITestOutputHelper _outputHelper;

    public SiloTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }
    private async Task<SiloRuntimeStatistics[]> GetStatistics(TestCluster cluster)
    {
        var mg = cluster.GrainFactory.GetGrain<IManagementGrain>(0);
        var statistics =  await mg.GetRuntimeStatistics(cluster.Silos.Select(s=>s.SiloAddress).ToArray());
        return statistics;
    }

    private async IAsyncEnumerable<Guid> ActivateGrains(TestCluster cluster, int number)
    {
        for (int i = 0; i < number; i++)
        {
            var hello = cluster.Client.GetGrain<ITestGrain>(Guid.NewGuid());
            var id = await hello.Do();
            yield return id;
        }
    }
    
    private async Task ReactivateGrains(TestCluster cluster, Guid[] guids)
    {
        int count = 0;
        _outputHelper.WriteLine($"ReactivateGrains for: {guids.Length} grains.");
        var sw = Stopwatch.StartNew();
        foreach (var item in guids)
        {
            do
            {
                try
                {
                    var hello = cluster.Client.GetGrain<ITestGrain>(item);
                    var id = await hello.Do();
                    break;
                }
                catch (Exception e)
                {
                    _outputHelper.WriteLine($"-------!!!!!!!!!!Exception");
                }
            } while (true);
           
            
            count++;
        }
        sw.Stop();
        _outputHelper.WriteLine($"ReactivateGrains for: {guids.Length}; count:{count}; time:{sw.Elapsed}");
    }
    private async Task SiloTest(int iteration, bool enableBalance)
    {
        var builder = new TestClusterBuilder();
        if(enableBalance)
            builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        
        builder.Options.InitialSilosCount = 1;
        
        var cluster = builder.Build();
        await cluster.DeployAsync();

        List<Guid> grains = new List<Guid>(iteration * 1000);
        List<Task> allTaks = new List<Task>(iteration);

        
        for (int i = 0; i < iteration; i++)
        {
            if (i % 20 == 0)
            {
                await cluster.StartAdditionalSiloAsync();
                _outputHelper.WriteLine("Cluster is added");
            }
            
            var sw = Stopwatch.StartNew();
            
            await foreach (var item in ActivateGrains(cluster, 1000))
            {
                grains.Add(item); 
            }
            sw.Stop();
            
            allTaks.Add(Task.Run(() => ReactivateGrains(cluster, grains.ToArray())));
            
            _outputHelper.WriteLine($"Interation:{i} - {sw.Elapsed}");
        }

        for (int i = 0; i < 5; i++)
        {
            var rsw = Stopwatch.StartNew();
            await ReactivateGrains(cluster, grains.ToArray());
            rsw.Stop();
            _outputHelper.WriteLine($"Reactivate - {i}");
        }


        await Task.WhenAll(allTaks);
        
        var stat = await GetStatistics(cluster);
        int count = 0;
        
        foreach (var silo in stat)
        {
            count++;
            _outputHelper.WriteLine($"Silo:{count}; ActivationCount:{silo.ActivationCount}; RecentlyUsedActivationCount:{silo.RecentlyUsedActivationCount}; SentMessages:{silo.SentMessages}");
            _outputHelper.WriteLine($"        SentMessages:{silo.SentMessages}; ReceivedMessages:{silo.ReceivedMessages};");
        }

        _outputHelper.WriteLine($"Total ActivationCount:{TestGrain.ActivationCount}");
        _outputHelper.WriteLine($"Total DeactivationCount:{TestGrain.DeactivationCount}");
        
    }
    
    [Fact]
    public async Task ClearSiloTest()
    {
        await SiloTest(80, false);
    }
    
    [Fact]
    public async Task BalancerSiloTest()
    {
        await SiloTest(80, true);
    }
}