using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Core;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

public class BalancerStartupTask : IStartupTask
{
    private readonly ILogger<BalancerStartupTask> _logger;
    private readonly IGrainFactory _grainFactory;
    private readonly IGrainRuntime _runtime;
    private readonly ILocalSiloDetails _localSiloDetails;
    private static HashSet<string> _grainTypes = new();
    private string[] _grainTypesArray;

    private static ConcurrentDictionary<string, WeakReference<Grain>> _grainsList = new();

    public static void AddGrain(IAddressable addressable)
    {
        if (addressable is Grain grain && _grainTypes.Contains(grain.GetType().FullName))
        {
            _grainsList[grain.IdentityString] = new WeakReference<Grain>(grain);
        }
    }
    
    public BalancerStartupTask(ILogger<BalancerStartupTask> logger, IGrainFactory grainFactory,  IGrainRuntime runtime,
        ILocalSiloDetails localSiloDetails)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _runtime = runtime;
        _localSiloDetails = localSiloDetails;
    }
    public Task Execute(CancellationToken cancellationToken)
    {
        _grainTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(CanDeactivate).Select(s=>s.FullName).ToHashSet();
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
            
            foreach (var item in _grainsList)
            {
                if (item.Value.TryGetTarget(out var grain))
                {
                    _runtime.DeactivateOnIdle(grain);
                }
                _grainsList.TryRemove(item);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(5), token);
        }
 
    }
    private static bool CanDeactivate(Type type)
    {
        return Attribute.GetCustomAttribute(type, typeof(CanDeactivateAttribute)) is not null;
    }
}


