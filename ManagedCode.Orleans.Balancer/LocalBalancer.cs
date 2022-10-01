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
    public ConcurrentDictionary<string, WeakReference<Grain>> GrainsList { get; } = new();

    public LocalBalancer(ILogger<LocalBalancer> logger, IGrainRuntime runtime)
    {
        _logger = logger;
        _runtime = runtime;
    }

    public void CheckDeactivation(IAddressable addressable)
    {
        if (addressable is not SystemTarget && addressable is Grain grain)
        {
            if (_deactivationNumber > 0)
            {
                Interlocked.Decrement(ref _deactivationNumber);
                _runtime.DeactivateOnIdle(grain);
            }
            else
            {
                GrainsList[grain.IdentityString] = new WeakReference<Grain>(grain);
            }
                
        }
    }

    public void SetDeactivationNumber(int number)
    {
        _deactivationNumber = number;
    }
}