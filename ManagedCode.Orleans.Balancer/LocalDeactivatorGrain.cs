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
    private readonly OrleansBalancerOptions _options;
    private readonly SiloAddress[] _localSiloAddresses;
    private readonly Dictionary<string, int> _grainTypes;

    public LocalDeactivatorGrain(
        ILocalSiloDetails siloDetails,
        IOptions<OrleansBalancerOptions> options)
    {
        _options = options.Value;
        _localSiloAddresses = new[] {siloDetails.SiloAddress};

        _grainTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(CanDeactivate)
            .Select(s => (s.FullName + "," + s.Assembly.GetName().Name, GetPriority(s)))
            .Distinct()
            .ToDictionary(s => s.Item1, s => s.Item2);

        foreach (var (key, value) in _options.GrainsForDeactivation)
        {
            _grainTypes.TryAdd(key, value);
        }
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        RegisterTimer(CheckAsync, null, _options.TimerIntervalDeactivation, _options.TimerIntervalDeactivation);

        return base.OnActivateAsync(cancellationToken);
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
        var countToDeactivate = (int) Math.Ceiling(localActivations * percentage);

        await DeactivateGrainsAsync(countToDeactivate);
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

        List<Task> deactivationTasks = new();

        foreach (var grainStatistic in grainStatistics.OrderBy(s => _grainTypes[s.GrainType]))
        {
            if (countToDeactivate == 0)
            {
                break;
            }

            var addressable = GrainFactory.GetGrain(grainStatistic.GrainId);
            deactivationTasks.Add(addressable.Cast<IGrainManagementExtension>().DeactivateOnIdle());

            countToDeactivate--;
        }

        var chunks = deactivationTasks
            .Chunk(_options.DeactivateGrainsAtTheSameTime)
            .Select(Task.WhenAll)
            .ToArray();

        foreach (var chunk in chunks)
        {
            await chunk;
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

    private static int GetPriority(Type type)
    {
        var attribute = Attribute.GetCustomAttribute(type, typeof(CanBeDeactivatedAttribute)) as CanBeDeactivatedAttribute;
        return attribute!.Priority;
    }
}