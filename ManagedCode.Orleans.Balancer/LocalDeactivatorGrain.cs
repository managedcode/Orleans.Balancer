using System.Runtime.CompilerServices;
using ManagedCode.Orleans.Balancer.Abstractions;
using ManagedCode.Orleans.Balancer.Attributes;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Core.Internal;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

[Reentrant]
[KeepAlive]
[LocalPlacement]
public class LocalDeactivatorGrain : Grain, ILocalDeactivatorGrain
{
    private readonly Dictionary<string, DeactivationPriority> _grainTypes;
    private readonly SiloAddress[] _localSiloAddresses;
    private readonly OrleansBalancerOptions _options;

    public LocalDeactivatorGrain(
        ILocalSiloDetails siloDetails,
        IOptions<OrleansBalancerOptions> options)
    {
        _options = options.Value;
        _localSiloAddresses = new[] { siloDetails.SiloAddress };

        _grainTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(CanDeactivate)
            .Select(s => (GetGrainType(s), GetPriority(s)))
            .Distinct()
            .ToDictionary(s => s.Item1, s => s.Item2);

        foreach (var (key, value) in _options.GrainsForDeactivation)
        {
            _grainTypes.TryAdd(GetGrainType(key), value);
        }
    }

    public Task InitializeAsync()
    {
        // Just for activate grain
        return Task.CompletedTask;
    }

    public async Task DeactivateGrainsAsync(float percentage)
    {
        if (percentage is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage));
        }

        var localActivations = await GetLocalActivationCount();
        var countToDeactivate = (int)Math.Ceiling(localActivations * percentage);

        await DeactivateGrainsAsync(countToDeactivate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetGrainType(Type type)
    {
        return type.FullName + "," + type.Assembly.GetName().Name;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        RegisterTimer(CheckAsync, null, _options.TimerIntervalDeactivation, _options.TimerIntervalDeactivation);

        return base.OnActivateAsync(cancellationToken);
    }

    private async Task CheckAsync(object obj)
    {
        var localActivations = await GetLocalActivationCount();

        if (localActivations >= _options.TotalGrainActivationsMinimumThreshold)
        {
            var countToDeactivate = localActivations - _options.TotalGrainActivationsMinimumThreshold;
            await DeactivateGrainsAsync(countToDeactivate);
        }
    }

    private async Task DeactivateGrainsAsync(int countToDeactivate)
    {
        var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);

        var grainStatistics =
            await managementGrain.GetDetailedGrainStatistics(_grainTypes.Select(s => s.Key).ToArray(), _localSiloAddresses);

        Queue<Func<Task>> deactivationTasks = new();

        foreach (var grainStatistic in grainStatistics.OrderBy(s => _grainTypes[s.GrainType]))
        {
            if (countToDeactivate == 0)
            {
                break;
            }

            var addressable = GrainFactory.GetGrain(grainStatistic.GrainId);
            deactivationTasks.Enqueue(() => addressable.Cast<IGrainManagementExtension>().DeactivateOnIdle());

            countToDeactivate--;
        }

        var chunks = deactivationTasks
            //.Chunk(_options.DeactivateGrainsAtTheSameTime);
            .Chunk(ThreadPool.ThreadCount / 4); //TODO: Experiment with TheadPool capacity;

        foreach (var chunk in chunks)
        {
            await Task.WhenAll(chunk.Select(s => s()));
            await Task.Delay(_options.DelayBetweenDeactivations);
        }
    }

    private async Task<int> GetLocalActivationCount()
    {
        var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);

        var localStatistics = await managementGrain.GetRuntimeStatistics(_localSiloAddresses);
        return localStatistics.Sum(s => s.ActivationCount);
    }

    private static bool CanDeactivate(Type type)
    {
        return Attribute.GetCustomAttribute(type, typeof(CanBeDeactivatedAttribute)) is not null;
    }

    private static DeactivationPriority GetPriority(Type type)
    {
        var attribute = Attribute.GetCustomAttribute(type, typeof(CanBeDeactivatedAttribute)) as CanBeDeactivatedAttribute;
        if (attribute is null)
        {
            return DeactivationPriority.Normal;
        }

        return attribute!.Priority;
    }
}