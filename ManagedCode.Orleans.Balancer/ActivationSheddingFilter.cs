using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.Balancer;

public sealed class ActivationSheddingFilter : IIncomingGrainCallFilter
{
    private readonly LocalBalancer _localBalancer;
    private readonly ILogger<ActivationSheddingFilter> _logger;

    public ActivationSheddingFilter(ILogger<ActivationSheddingFilter> logger, LocalBalancer localBalancer)
    {
        _logger = logger;
        _localBalancer = localBalancer;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        await context.Invoke();
        _localBalancer.CheckDeactivation(context.Grain);
    }
}