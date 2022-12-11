using Orleans;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;

[CanBeDeactivated]
public class TestGrain : Grain, ITestGrain
{
    public static int ActivationCount;
    public static int DeactivationCount;

    public Task<Guid> Do()
    {
        return Task.FromResult(this.GetPrimaryKey());
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