using System.Diagnostics;
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
    private volatile int _errors;

    public SiloTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    private async Task<SiloRuntimeStatistics[]> GetStatistics(TestCluster cluster)
    {
        var mg = cluster.GrainFactory.GetGrain<IManagementGrain>(0);
        var statistics = await mg.GetRuntimeStatistics(cluster.Silos.Select(s => s.SiloAddress).ToArray());
        return statistics;
    }

    private async IAsyncEnumerable<Guid> ActivateGrains(TestCluster cluster, int number)
    {
        for (var i = 0; i < number; i++)
        {
            var hello = cluster.Client.GetGrain<ITestGrain>(Guid.NewGuid());
            var id = await hello.Do();
            yield return id;
        }
    }

    private async Task ReactivateGrains(TestCluster cluster, Guid[] guids)
    {
        var count = 0;
        if (Debugger.IsAttached)
        {
            _outputHelper.WriteLine($"ReactivateGrains for: {guids.Length} grains.");
        }

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
                    Interlocked.Increment(ref _errors);
                    if (Debugger.IsAttached)
                    {
                        _outputHelper.WriteLine("-------!!!!!!!!!!Exception");
                    }
                }
            } while (true);

            count++;
        }

        sw.Stop();

        if (Debugger.IsAttached)
        {
            _outputHelper.WriteLine($"ReactivateGrains for: {guids.Length}; count:{count}; time:{sw.Elapsed}");
        }
    }

    private async Task SiloTest(int iteration, bool enableBalance)
    {
        var builder = new TestClusterBuilder();
        if (enableBalance)
        {
            builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        }

        builder.Options.InitialSilosCount = 1;

        var cluster = builder.Build();
        await cluster.DeployAsync();

        var grains = new List<Guid>(iteration * 1000);
        var allTaks = new List<Task>(iteration);

        for (var i = 0; i < iteration; i++)
        {
            if (i % 20 == 0)
            {
                await cluster.StartAdditionalSiloAsync();
                _outputHelper.WriteLine("Cluster is added - " + i);
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

        for (var i = 0; i < 5; i++)
        {
            var rsw = Stopwatch.StartNew();
            await ReactivateGrains(cluster, grains.ToArray());
            rsw.Stop();
            _outputHelper.WriteLine($"Reactivate - {i}");
        }

        await Task.WhenAll(allTaks);

        var stat = await GetStatistics(cluster);
        var count = 0;

        foreach (var silo in stat)
        {
            count++;
            _outputHelper.WriteLine(
                $"Silo:{count}; ActivationCount:{silo.ActivationCount}; RecentlyUsedActivationCount:{silo.RecentlyUsedActivationCount}; SentMessages:{silo.SentMessages}");
            _outputHelper.WriteLine($"        SentMessages:{silo.SentMessages}; ReceivedMessages:{silo.ReceivedMessages};");
        }

        _outputHelper.WriteLine($"Total ActivationCount:{TestGrain.ActivationCount}");
        _outputHelper.WriteLine($"Total DeactivationCount:{TestGrain.DeactivationCount}");
        _outputHelper.WriteLine($"Total ERRORS:{_errors}");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(80)]
    [InlineData(100)]
    public async Task ClearSiloTest(int itereations)
    {
        _errors = 0;
        await SiloTest(itereations, false);
    }


    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(80)]
    [InlineData(100)]
    public async Task BalancerSiloTest(int itereations)
    {
        _errors = 0;
        await SiloTest(itereations, true);
    }

    [Theory]
    [InlineData(250_000)]
    [InlineData(500_000)]
    public async Task ClearSiloSingleThreadTest(int itereations)
    {
        _errors = 0;
        await SingleThread(itereations, false);
    }

    [Theory]
    [InlineData(250_000)]
    [InlineData(500_000)]
    public async Task BalancerSiloSingleThreadTest(int itereations)
    {
        _errors = 0;
        await SingleThread(itereations, true);
    }

    private async Task SingleThread(int iteration, bool enableBalance)
    {
        _errors = 0;
        var builder = new TestClusterBuilder();
        if (enableBalance)
        {
            builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        }

        builder.Options.InitialSilosCount = 1;

        var cluster = builder.Build();
        await cluster.DeployAsync();

        var rnd = new Random();
        for (var i = 0; i < iteration; i++)
        {
            if (i % 100_000 == 0)
            {
                await cluster.StartAdditionalSiloAsync();
                _outputHelper.WriteLine("Cluster is added - " + i);
            }

            if (rnd.Next(0, 1) == 0)
            {
                var hello = cluster.Client.GetGrain<ITestGrainInt>(rnd.Next(0, 30_000));
                try
                {
                    var id = await hello.Do();
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref _errors);
                    if (Debugger.IsAttached)
                    {
                        _outputHelper.WriteLine($"!!!Error:{e.Message}");
                    }
                }
            }
            else
            {
                var hello = cluster.Client.GetGrain<ITestGrain>(Guid.NewGuid());
                try
                {
                    var id = await hello.Do();
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref _errors);
                    if (Debugger.IsAttached)
                    {
                        _outputHelper.WriteLine($"!!!Error:{e.Message}");
                    }
                }
            }
        }

        var stat = await GetStatistics(cluster);
        var count = 0;

        foreach (var silo in stat)
        {
            count++;
            _outputHelper.WriteLine(
                $"Silo:{count}; ActivationCount:{silo.ActivationCount}; RecentlyUsedActivationCount:{silo.RecentlyUsedActivationCount}; SentMessages:{silo.SentMessages}");
            _outputHelper.WriteLine($"        SentMessages:{silo.SentMessages}; ReceivedMessages:{silo.ReceivedMessages};");
        }

        _outputHelper.WriteLine($"Total ActivationCount:{TestGrainInt.ActivationCount}");
        _outputHelper.WriteLine($"Total DeactivationCount:{TestGrainInt.DeactivationCount}");
        _outputHelper.WriteLine($"Total ERRORS:{_errors}");
    }

    [Fact]
    public async Task SchreddingGrains()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.Options.InitialSilosCount = 3;

        var cluster = builder.Build();
        await cluster.DeployAsync();

        for (var i = 0; i < 10; i++)
        {
            var hello1 = await cluster.Client.GetGrain<ITestGrainInt>(i).Do();
            var hello2 = await cluster.Client.GetGrain<ITestGrain>(Guid.NewGuid()).Do();
        }

        var grain = cluster.Client.GetGrain<IManagementGrain>(0);
        var hosts = await grain.GetHosts();
        var xx1 = await grain.GetSimpleGrainStatistics();
        var xx2 = await grain.GetRuntimeStatistics(new[] {hosts.First().Key});
        var xx3 = await grain.GetDetailedGrainStatistics(hostsIds: new[] {hosts.First().Key});

        await Task.Delay(TimeSpan.FromSeconds(10));

        for (var i = 0; i < 10; i++)
        {
            var hello1 = await cluster.Client.GetGrain<ITestGrainInt>(i).Do();
            var hello2 = await cluster.Client.GetGrain<ITestGrain>(Guid.NewGuid()).Do();
        }

        await Task.Delay(TimeSpan.FromSeconds(10));

        for (var i = 0; i < 10; i++)
        {
            var hello1 = await cluster.Client.GetGrain<ITestGrainInt>(i).Do();
            var hello2 = await cluster.Client.GetGrain<ITestGrain>(Guid.NewGuid()).Do();
        }

        await Task.Delay(TimeSpan.FromSeconds(30));

        xx1 = await grain.GetSimpleGrainStatistics();
        xx2 = await grain.GetRuntimeStatistics(new[] {hosts.First().Key});
        xx3 = await grain.GetDetailedGrainStatistics(hostsIds: new[] {hosts.First().Key});

        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }
}