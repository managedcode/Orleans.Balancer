using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

public class BalancerStartupTask : IStartupTask
{
    private static HashSet<string> _grainTypes = new();
    private readonly IGrainFactory _grainFactory;
    private readonly LocalGrainHolder _localGrainHolder;
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly ILogger<BalancerStartupTask> _logger;
    private readonly IGrainRuntime _runtime;
    private string[] _grainTypesArray;

    public BalancerStartupTask(ILogger<BalancerStartupTask> logger,
        IGrainFactory grainFactory,
        IGrainRuntime runtime,
        ILocalSiloDetails localSiloDetails,
        LocalGrainHolder localGrainHolder)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _runtime = runtime;
        _localSiloDetails = localSiloDetails;
        _localGrainHolder = localGrainHolder;
    }

    public Task Execute(CancellationToken cancellationToken)
    {
        _grainTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(CanDeactivate)
            .Select(s => s.FullName)
            .ToHashSet();
        _grainTypesArray = _grainTypes.ToArray();

        BackgroundWork(cancellationToken).Ignore();
        return Task.CompletedTask;
    }

    private async Task BackgroundWork(CancellationToken token)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), token);
        while (!token.IsCancellationRequested)
        {
            var managementGrain = _grainFactory.GetGrain<IManagementGrain>(0);
            //var runtimeStatistics= await managementGrain.GetRuntimeStatistics(new []{ _localSiloDetails.SiloAddress });
            //var detailedGrainStatistics= await managementGrain.GetDetailedGrainStatistics(_grainTypesArray, new []{ _localSiloDetails.SiloAddress });

            /* foreach (var statistic in runtimeStatistics)
            {
                //statistic.ActivationCount
            }
            
            foreach (var statistic in detailedGrainStatistics)
            {
                

            }*/

            foreach (var item in _localGrainHolder.GrainsList)
            {
                if (item.Value.TryGetTarget(out var grain))
                {
                    _runtime.DeactivateOnIdle(grain);
                }

                _localGrainHolder.GrainsList.TryRemove(item);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), token);
        }
    }

    private static bool CanDeactivate(Type type)
    {
        return Attribute.GetCustomAttribute(type, typeof(CanDeactivateAttribute)) is not null;
    }
}