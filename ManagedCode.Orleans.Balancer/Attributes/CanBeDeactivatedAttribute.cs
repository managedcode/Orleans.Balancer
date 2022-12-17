namespace ManagedCode.Orleans.Balancer.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CanBeDeactivatedAttribute : Attribute
{
    public CanBeDeactivatedAttribute()
    {
    }

    public CanBeDeactivatedAttribute(DeactivationPriority priority)
    {
        Priority = priority;
    }

    public DeactivationPriority Priority { get; set; } = DeactivationPriority.Normal;
}