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

    public BalancerGrain(IOptions<OrleansBalancerOptions> options)
    {
        _options = options.Value;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
        var hosts = await managementGrain.GetHosts(true);
        _silos = hosts.Select(s => s.Key).ToArray();

        RegisterTimer(CheckAsync, null, _options.TimerInterval, _options.TimerInterval);
    }

    private async Task CheckAsync(object obj)
    {
        var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
        var hosts = await managementGrain.GetHosts(true);

        if (hosts.Count > _silos.Length)
        {
            _ = RebalanceAsync(_silos);
        }

        _silos = hosts.Select(s => s.Key).ToArray();
    }

    public async Task RebalanceAsync(IEnumerable<SiloAddress> silos)
    {
        var tasks = silos
            .Select(address => GrainFactory.GetGrain<ILocalDeactivatorGrain>(address.ToParsableString()))
            .Select(grain => grain.DeactivateGrainsAsync(0.33f))
            .ToList();

        await Task.WhenAll(tasks);
    }

    public Task InitializeAsync()
    {
        // Just for activate grain
        return Task.CompletedTask;
    }
}