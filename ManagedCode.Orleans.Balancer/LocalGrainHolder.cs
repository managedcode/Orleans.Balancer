using System.Collections.Concurrent;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

public sealed class LocalGrainHolder
{
    public ConcurrentDictionary<string, WeakReference<Grain>> GrainsList { get; } = new();

    public void AddGrain(IAddressable addressable)
    {
        if (addressable is Grain grain)
        {
            GrainsList[grain.IdentityString] = new WeakReference<Grain>(grain);
        }
    }
}