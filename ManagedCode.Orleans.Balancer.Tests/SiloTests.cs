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

    private async Task RunTestGrain(TestCluster cluster, int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            var testGrain = cluster.Client.GetGrain<ITestGrain>(Guid.NewGuid());
            var testGrainInt = cluster.Client.GetGrain<ITestGrainInt>(i);
            var testGrainString = cluster.Client.GetGrain<ITestGrainString>(i.ToString());

            await Task.WhenAll(testGrain.Do(), testGrainInt.Do(), testGrainString.Do());
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

        var total = iteration / 10;
        for (var loop = 0; loop < total; loop++)
        {
            var sw = Stopwatch.StartNew();
            Parallel.For(1, iteration * 10, i => { Task.WaitAll(RunTestGrain(cluster, iteration)); });
            sw.Stop();

            _outputHelper.WriteLine($"Interation:{loop}/{total} - {sw.Elapsed}");
            await cluster.StartAdditionalSiloAsync();
        }

        await Task.Delay(TimeSpan.FromSeconds(30));

        var final = Stopwatch.StartNew();
        Parallel.For(1, iteration * 10, i => { Task.WaitAll(RunTestGrain(cluster, iteration)); });
        final.Stop();

        _outputHelper.WriteLine($"Interation:Final - {final.Elapsed}");

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
    [InlineData(50)]
    //[InlineData(100)]
    public async Task ClearSiloTest(int itereations)
    {
        _errors = 0;
        await SiloTest(itereations, false);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    //[InlineData(100)]
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

            var newRnd = rnd.Next(0, 2);
            if (newRnd == 0)
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
            else if (newRnd == 1)
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
            else
            {
                var hello = cluster.Client.GetGrain<ITestGrainString>(rnd.Next(0, 30_000).ToString());
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
        var xx2 = await grain.GetRuntimeStatistics(new[] { hosts.First().Key });
        var xx3 = await grain.GetDetailedGrainStatistics(hostsIds: new[] { hosts.First().Key });

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
        xx2 = await grain.GetRuntimeStatistics(new[] { hosts.First().Key });
        xx3 = await grain.GetDetailedGrainStatistics(hostsIds: new[] { hosts.First().Key });

        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }
}