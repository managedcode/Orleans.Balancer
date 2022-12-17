using ManagedCode.Orleans.Balancer.Attributes;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;

[CanBeDeactivated(DeactivationPriority.Low)]
public class TestGrainString : Grain, ITestGrainString
{
    public static int ActivationCount;
    public static int DeactivationCount;

    public Task<GrainId> Do()
    {
        return Task.FromResult(this.GetGrainId());
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref ActivationCount);
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref DeactivationCount);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}