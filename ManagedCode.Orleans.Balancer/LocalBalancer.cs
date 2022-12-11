using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

public sealed class LocalBalancer
{
    private readonly ILogger<LocalBalancer> _logger;
    private readonly IGrainRuntime _runtime;
    private volatile int _deactivationNumber;
    public ConcurrentDictionary<string ,WeakReference<Grain>> GrainsList { get; } = new();

    public LocalBalancer(ILogger<LocalBalancer> logger, IGrainRuntime runtime)
    {
        _logger = logger;
        _runtime = runtime;
    }

    public void CheckDeactivation(IGrainContext addressable)
    {
        if (addressable is not SystemTarget && addressable is Grain grain && CanDeactivate(grain.GetType()))
        {
            if (_deactivationNumber > 0)
            {
                Interlocked.Decrement(ref _deactivationNumber);
                _runtime.DeactivateOnIdle(addressable);
            }
            else
            {
                //var reference = new WeakReference<Grain>(grain);
                //GrainsList[grain.IdentityString] = reference;
            }
        }
    }

    public void SetDeactivationNumber(int number)
    {
        _deactivationNumber = number;
    }
    
    private bool CanDeactivate(Type type)
    {
        return Attribute.GetCustomAttribute(type, typeof(CanBeDeactivatedAttribute)) is not null;
    }
}