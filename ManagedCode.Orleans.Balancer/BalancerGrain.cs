using ManagedCode.Orleans.Balancer.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

[KeepAlive]
[Reentrant]
public class BalancerGrain : Grain, IBalancerGrain
{
    private readonly OrleansBalancerOptions _options;
    private SiloAddress[] _silos = Array.Empty<SiloAddress>();
    private bool _initialized;

    public BalancerGrain(IOptions<OrleansBalancerOptions> options)
    {
        _options = options.Value;
    }

    public Task<bool> InitializeAsync()
    {
        // First call will return true, others will return false;
        
        if (_initialized)
            return Task.FromResult(false);

        _initialized = true;
        return Task.FromResult(_initialized);
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
        var hosts = await managementGrain.GetHosts(true);
        _silos = hosts.Select(s => s.Key).ToArray();

        RegisterTimer(CheckAsync, null, _options.TimerIntervalRebalancing, _options.TimerIntervalRebalancing);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    private async Task CheckAsync(object obj)
    {
        var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
        var hosts = await managementGrain.GetHosts(true);

        if (hosts.Count > _silos.Length)
        {
            _ = ReBalanceAsync(_silos);
        }

        _silos = hosts.Select(s => s.Key).ToArray();
    }

    public async Task ReBalanceAsync(IEnumerable<SiloAddress> silos)
    {
        var tasks = silos
            .Select(address => GrainFactory.GetGrain<ILocalDeactivatorGrain>(address.ToParsableString()))
            .Select(grain => grain.DeactivateGrainsAsync(_options.ReBalancingPercentage))
            .ToList();

        await Task.WhenAll(tasks);
    }
}