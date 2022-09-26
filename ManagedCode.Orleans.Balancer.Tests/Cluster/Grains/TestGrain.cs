using Orleans;

namespace ManagedCode.Orleans.Balancer.Tests.Cluster.Grains;

[CanDeactivate]
public class TestGrain : Grain, ITestGrain
{
    public Task<Guid> Do()
    {
        return Task.FromResult(this.GetPrimaryKey());
    }

    public override Task OnActivateAsync()
    {
        Interlocked.Increment(ref ActivationCount);
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        Interlocked.Increment(ref DeactivationCount);
        return base.OnDeactivateAsync();
    }

    public static int ActivationCount;
    public static int DeactivationCount;
}

[CanDeactivate]
public class TestGrainInt : Grain, ITestGrainInt
{
    public Task<int> Do()
    {
        return Task.FromResult((int)this.GetPrimaryKeyLong());
    }

    public override Task OnActivateAsync()
    {
        Interlocked.Increment(ref ActivationCount);
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        Interlocked.Increment(ref DeactivationCount);
        return base.OnDeactivateAsync();
    }

    public static int ActivationCount;
    public static int DeactivationCount;
}