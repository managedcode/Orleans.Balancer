using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;

//[CanBeDeactivated] from config, priory High
public class TestGrainInt : Grain, ITestGrainInt
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