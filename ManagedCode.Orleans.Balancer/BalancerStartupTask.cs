using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

public class BalancerStartupTask : IStartupTask
{
    private HashSet<string> _grainTypes = new();
    private readonly IGrainFactory _grainFactory;
    private readonly LocalGrainHolder _localGrainHolder;
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly ILogger<BalancerStartupTask> _logger;
    private readonly ActivationSheddingOptions _options;
    private readonly IGrainRuntime _runtime;
    private string[] _grainTypesArray;

    public BalancerStartupTask(ILogger<BalancerStartupTask> logger,
        IOptions<ActivationSheddingOptions> options,
        IGrainFactory grainFactory,
        IGrainRuntime runtime,
        ILocalSiloDetails localSiloDetails,
        LocalGrainHolder localGrainHolder)
    {
        _logger = logger;
        _options = options.Value;
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
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.TimerIntervalSeconds), token);
            
            var managementGrain = _grainFactory.GetGrain<IManagementGrain>(0);
            var runtimeStatistics = await managementGrain.GetSimpleGrainStatistics();
                
            // await managementGrain.GetRuntimeStatistics(new []{ _localSiloDetails.SiloAddress });
            //var detailedGrainStatistics= await managementGrain.GetDetailedGrainStatistics(_grainTypesArray, new []{ _localSiloDetails.SiloAddress });

            var totalActivations = runtimeStatistics.Sum(s=>s.ActivationCount);
            var myActivations = _localGrainHolder.GrainsList.Count;
            double myPercentage;
            double targetPercentage;
            double overagePercentTrigger;
            double overagePercent;
            
            
            myPercentage = Math.Floor((double)myActivations / totalActivations * 100);

            // e.g. for three silos, 33% - the average each should aim to have
            targetPercentage = Math.Floor(100d /runtimeStatistics.Length);

            // e.g. 20% overage = 33% (1/3) + 20% = 53% would be the trigger
            overagePercentTrigger = _options.BaselineTriggerPercentage;
            
            if (runtimeStatistics.Length > 2)
            {
                overagePercentTrigger = overagePercentTrigger * (1 + (2 - runtimeStatistics.Length) * 0.2);
                if (overagePercentTrigger < 2)
                {
                    overagePercentTrigger = 2;
                }
            }

            overagePercent = Math.Floor(myPercentage - targetPercentage);
            
            var averageActivationsPerSilo = Math.Floor((double)totalActivations / runtimeStatistics.Length);

            // update counter (i.e. we set the "recovery" point at 95% of the overage activations beyond target)
            var surplusActivations = (int)Math.Floor((myActivations - averageActivationsPerSilo) * _options.LowerRecoveryThresholdFactor);
            
            // validate against an absolute threshold for cluster-level activations
            if (totalActivations > _options.TotalGrainActivationsMinimumThreshold)
            {
                // am I above the average expected?  (e.g. > 33% for 3 silos)
                if (myPercentage > targetPercentage)
                {
                    // by how much? are we over by more than the overage trigger?
                    if (overagePercent >= overagePercentTrigger)
                    {
                        EmitReBalancingEvent(totalActivations,
                            myActivations,
                            overagePercent,
                            overagePercentTrigger,
                            runtimeStatistics.Length,
                            surplusActivations,
                            _localSiloDetails.SiloAddress,
                            StartEvent);
                        
                        foreach (var item in _localGrainHolder.GrainsList)
                        {
                            if (item.Value.TryGetTarget(out var grain))
                            {
                                _runtime.DeactivateOnIdle(grain);
                                surplusActivations--;
                            }

                            _localGrainHolder.GrainsList.TryRemove(item);
                            
                            if(surplusActivations == 0)
                                break;
                        }
                        
                        // only emit event if not already rebalancing
                        EmitReBalancingEvent(totalActivations,
                            myActivations,
                            overagePercent,
                            overagePercentTrigger,
                            runtimeStatistics.Length,
                            surplusActivations,
                            _localSiloDetails.SiloAddress,
                            StopEvent);

                      
                        
                    }
                }
            }

            
        }
    }
    
    
    private static readonly EventId StartEvent = new(58001, "Starting");
    private static readonly EventId SheddingEvent = new(58002, "Shedding");
    private static readonly EventId StopEvent = new(58003, "Stopping");
    
    private void EmitReBalancingEvent(int totalActivations,
        int myActivations,
        double overagePercent,
        double overagePercentTrigger,
        int activeSilosCount,
        int surplusActivations,
        SiloAddress localSilo,
        EventId phase)
    {
        var customDimensions = new Dictionary<string, string>
        {
            { "orleans.silo.rebalancingPhase", phase.Name }, // started -> shedding -> stopped
            { "orleans.silo", $"{localSilo.ToLongString()}" },
            { "orleans.cluster.siloCount", activeSilosCount.ToString() },
            { "orleans.cluster.totalActivations", totalActivations.ToString() },
            { "orleans.silo.activations", myActivations.ToString() },
            { "orleans.silo.activationsToCut", surplusActivations.ToString() },
            { "orleans.silo.overagePercent", $"{overagePercent}%" },
            { "orleans.silo.overageThresholdPercent", $"{overagePercentTrigger}%" }
        };

        _logger.LogInformation(phase, $"Silo Activation Shedding {customDimensions}");
    }
    
    private bool CanDeactivate(Type type)
    {
        return Attribute.GetCustomAttribute(type, typeof(CanDeactivateAttribute)) is not null;
    }
}