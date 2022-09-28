using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.Balancer;

public sealed class ActivationSheddingFilter : IIncomingGrainCallFilter
{
    private readonly ILogger<ActivationSheddingFilter> _logger;
    private readonly LocalGrainHolder _localGrainHolder;
    public ActivationSheddingFilter(ILogger<ActivationSheddingFilter> logger, LocalGrainHolder localGrainHolder)
    {
        _logger = logger;
        _localGrainHolder = localGrainHolder;
    }
    
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        await context.Invoke();
        _localGrainHolder.AddGrain(context.Grain);
    }
}