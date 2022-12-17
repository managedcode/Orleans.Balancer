using System.ComponentModel.DataAnnotations;
using ManagedCode.Orleans.Balancer.Attributes;

namespace ManagedCode.Orleans.Balancer;

public record OrleansBalancerOptions
{
    public Dictionary<Type, DeactivationPriority> GrainsForDeactivation = new();
    public int TotalGrainActivationsMinimumThreshold { get; set; } = 5000;

    public int DeactivateGrainsAtTheSameTime { get; set; } = 100;

    public TimeSpan DelayBetweenDeactivations { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan TimerIntervalDeactivation { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan TimerIntervalRebalancing { get; set; } = TimeSpan.FromSeconds(10);

    [Range(0f, 1f)]
    public float ReBalancingPercentage { get; set; } = 0.33f;
}