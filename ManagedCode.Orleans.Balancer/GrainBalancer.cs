// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Orleans.Core.Internal;
// using Orleans.Runtime;
//
// namespace ManagedCode.Orleans.Balancer;
//
// public class GrainBalancer : IStartupTask
// {
//     private static readonly EventId StartEvent = new(58001, "Starting");
//     private static readonly EventId SheddingEvent = new(58002, "Shedding");
//     private static readonly EventId StopEvent = new(58003, "Stopping");
//     private readonly IGrainFactory _grainFactory;
//     private readonly ILocalSiloDetails _localSiloDetails;
//     private readonly ILogger<GrainBalancer> _logger;
//     private readonly OrleansBalancerOptions _options;
//     private readonly IGrainRuntime _runtime;
//     private readonly string[] _grainTypes;
//
//     public GrainBalancer(ILogger<GrainBalancer> logger,
//         IOptions<OrleansBalancerOptions> options,
//         IGrainFactory grainFactory,
//         IGrainRuntime runtime,
//         ILocalSiloDetails localSiloDetails)
//     {
//         _logger = logger;
//         _options = options.Value;
//         _grainFactory = grainFactory;
//         _runtime = runtime;
//         _localSiloDetails = localSiloDetails;
//
//         _grainTypes = AppDomain.CurrentDomain.GetAssemblies()
//             .SelectMany(s => s.GetTypes())
//             .Where(CanDeactivate)
//             .Select(s => s.FullName + "," + s.Assembly.GetName().Name)
//             .Distinct()
//             .ToArray();
//     }
//
//     public Task Execute(CancellationToken cancellationToken)
//     {
//         //ManagedCode.Orleans.Balancer.Tests.Cluster.Grains.TestGrain,ManagedCode.Orleans.Balancer.Tests
//         BackgroundWork(cancellationToken).Ignore();
//         return Task.CompletedTask;
//     }
//
//     private async Task BackgroundWork(CancellationToken token)
//     {
//         while (!token.IsCancellationRequested)
//         {
//             await Task.Delay(TimeSpan.FromSeconds(_options.TimerIntervalSeconds), token);
//
//             var managementGrain = _grainFactory.GetGrain<IManagementGrain>(0);
//             var simpleGrainStatistics = await managementGrain.GetSimpleGrainStatistics();
//
//             var silos = await managementGrain.GetHosts(true);
//
//             var localStatistics = await managementGrain.GetRuntimeStatistics(new[] {_localSiloDetails.SiloAddress});
//
//             var localActivations = localStatistics.Sum(s => s.ActivationCount);
//             var totalActivations = await managementGrain.GetTotalActivationCount();
//             var myActivations = _localBalancer.GrainsList.Count;
//
//             var myPercentage = Math.Floor((double) localActivations / totalActivations * 100);
//
//             // e.g. for three silos, 33% - the average each should aim to have
//             var targetPercentage = Math.Floor(100d / simpleGrainStatistics.Length);
//
//             // e.g. 20% overage = 33% (1/3) + 20% = 53% would be the trigger
//             double overagePercentTrigger = _options.BaselineTriggerPercentage;
//
//             if (simpleGrainStatistics.Length > 2)
//             {
//                 overagePercentTrigger = overagePercentTrigger * (1 + (2 - simpleGrainStatistics.Length) * 0.2);
//                 if (overagePercentTrigger < 2)
//                 {
//                     overagePercentTrigger = 2;
//                 }
//             }
//             
//             await managementGrain.ForceActivationCollection(new[] {_localSiloDetails.SiloAddress}, TimeSpan.FromMinutes(1));
//
//             var detailedGrainStatistics1 = await managementGrain.GetDetailedGrainStatistics(_grainTypes, new[] {_localSiloDetails.SiloAddress});
//             foreach (var grainInfo in detailedGrainStatistics1)
//             {
//                 var addressable = _runtime.GrainFactory.GetGrain(grainInfo.GrainId);
//                 await addressable.Cast<IGrainManagementExtension>().DeactivateOnIdle();
//             }
//
//
//             var overagePercent = Math.Floor(myPercentage - targetPercentage);
//
//             var averageActivationsPerSilo = Math.Floor((double) totalActivations / simpleGrainStatistics.Length);
//
//             // update counter (i.e. we set the "recovery" point at 95% of the overage activations beyond target)
//             var surplusActivations = (int) Math.Floor((myActivations - averageActivationsPerSilo) * _options.LowerRecoveryThresholdFactor);
//
//             // validate against an absolute threshold for cluster-level activations
//             if (totalActivations > _options.TotalGrainActivationsMinimumThreshold)
//             {
//                 // am I above the average expected?  (e.g. > 33% for 3 silos)
//                 if (myPercentage > targetPercentage)
//                 {
//                     // by how much? are we over by more than the overage trigger?
//                     if (overagePercent >= overagePercentTrigger)
//                     {
//                         EmitReBalancingEvent(totalActivations,
//                             myActivations,
//                             overagePercent,
//                             overagePercentTrigger,
//                             simpleGrainStatistics.Length,
//                             surplusActivations,
//                             _localSiloDetails.SiloAddress,
//                             StartEvent);
//
//
//                         // var detailedGrainStatistics= await managementGrain.GetDetailedGrainStatistics(_grainTypesArray, new []{ _localSiloDetails.SiloAddress });
//                         //
//                         // foreach (var grainInfo in detailedGrainStatistics)
//                         // {
//                         //     surplusActivations--;
//                         //
//                         //     var addressable = _runtime.GrainFactory.GetGrain(grainInfo.GrainId);
//                         //     _grainContextActivatorProvider.TryGet(GrainType.Create(grainInfo.GrainType), out var cc);
//                         //     cc.CreateContext(new GrainAddress()
//                         //     {
//                         //         GrainId = grainInfo.GrainId;
//                         //     });
//                         //     
//                         //     if (addressable is not null)
//                         //     {
//                         //         var grain = addressable.AsReference(Type.GetType(grainInfo.GrainType));
//                         //         var gg = grain as Grain;
//                         //         _runtime.DeactivateOnIdle(grain as IGrainContext);
//                         //         if(surplusActivations == 0)
//                         //             break;
//                         //     }
//                         //
//                         // }
//
//
//                         //_localBalancer.SetDeactivationNumber(surplusActivations);
//
//                         /*
//                         foreach (var item in _localBalancer.GrainsList)
//                         {
//                             if (item.Value.TryGetTarget(out var grain))
//                             {
//                                 _runtime.DeactivateOnIdle(grain);
//                                 surplusActivations--;
//                             }
//
//                             _localBalancer.GrainsList.TryRemove(item);
//
//                             if (surplusActivations <= 0)
//                             {
//                                 break;
//                             }
//                         }*/
//
//                         // only emit event if not already rebalancing
//                         EmitReBalancingEvent(totalActivations,
//                             myActivations,
//                             overagePercent,
//                             overagePercentTrigger,
//                             simpleGrainStatistics.Length,
//                             surplusActivations,
//                             _localSiloDetails.SiloAddress,
//                             StopEvent);
//                     }
//                 }
//             }
//         }
//         
//     }
//
//     private void EmitReBalancingEvent(int totalActivations,
//         int myActivations,
//         double overagePercent,
//         double overagePercentTrigger,
//         int activeSilosCount,
//         int surplusActivations,
//         SiloAddress localSilo,
//         EventId phase)
//     {
//         var customDimensions = new Dictionary<string, string>
//         {
//             {"orleans.silo.rebalancingPhase", phase.Name}, // started -> shedding -> stopped
//             {"orleans.silo", $"{localSilo.ToParsableString()}"},
//             {"orleans.cluster.siloCount", activeSilosCount.ToString()},
//             {"orleans.cluster.totalActivations", totalActivations.ToString()},
//             {"orleans.silo.activations", myActivations.ToString()},
//             {"orleans.silo.activationsToCut", surplusActivations.ToString()},
//             {"orleans.silo.overagePercent", $"{overagePercent}%"},
//             {"orleans.silo.overageThresholdPercent", $"{overagePercentTrigger}%"}
//         };
//
//         _logger.LogInformation(phase, $"Silo Activation Shedding {customDimensions}");
//     }
//
//     private static bool CanDeactivate(Type type)
//     {
//         return Attribute.GetCustomAttribute(type, typeof(CanBeDeactivatedAttribute)) is not null;
//     }
// }