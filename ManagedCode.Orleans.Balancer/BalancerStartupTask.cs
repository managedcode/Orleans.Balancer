using ManagedCode.Orleans.Balancer.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

public class BalancerStartupTask : IStartupTask
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly ILogger<BalancerStartupTask> _logger;
    
    public BalancerStartupTask(ILogger<BalancerStartupTask> logger, IGrainFactory grainFactory, ILocalSiloDetails localSiloDetails)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _localSiloDetails = localSiloDetails;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        await InitializeGrainsAsync();
        _ = Task.Run(ExecuteInternal);
    }

    private async Task ExecuteInternal()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(2));
            await InitializeGrainsAsync();
        }
    }

    private async Task InitializeGrainsAsync()
    {
        try
        {
            await _grainFactory.GetGrain<IBalancerGrain>(0).InitializeAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "IBalancerGrain throw an error.");
        }
        
        try
        {
            await _grainFactory.GetGrain<ILocalDeactivatorGrain>(_localSiloDetails.SiloAddress.ToParsableString()).InitializeAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ILocalDeactivatorGrain throw an error.");
        }

  
    }
}