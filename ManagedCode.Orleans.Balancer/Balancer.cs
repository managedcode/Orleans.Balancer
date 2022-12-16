using ManagedCode.Orleans.Balancer.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

public class Balancer : IStartupTask
{
    private readonly ILogger<Balancer> _logger;
    private readonly IGrainFactory _grainFactory;
    private readonly ILocalSiloDetails _localSiloDetails;

    public Balancer(ILogger<Balancer> logger, IGrainFactory grainFactory, ILocalSiloDetails localSiloDetails)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _localSiloDetails = localSiloDetails;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            ActivateLocalDeactivatorGrainAsync(),
            ActivateBalancerGrainAsync());
    }

    private async Task ActivateLocalDeactivatorGrainAsync()
    {
        var siloGrain = _grainFactory.GetGrain<ILocalDeactivatorGrain>(_localSiloDetails.SiloAddress.ToParsableString());
        await siloGrain.InitializeAsync();
    }

    private async Task ActivateBalancerGrainAsync()
    {
        var dashboardGrain = _grainFactory.GetGrain<IBalancerGrain>(0);
        await dashboardGrain.InitializeAsync();
    }
}