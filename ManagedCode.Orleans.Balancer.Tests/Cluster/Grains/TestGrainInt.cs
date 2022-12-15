using ManagedCode.Orleans.Balancer.Attributes;
using Orleans;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;

[CanBeDeactivated]
public class TestGrainInt : Grain, ITestGrainInt
{
    public static int ActivationCount;
    public static int DeactivationCount;

    public Task<int> Do()
    {
        return Task.FromResult((int)this.GetPrimaryKeyLong());
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