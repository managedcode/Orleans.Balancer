namespace ManagedCode.Orleans.Balancer.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CanBeDeactivatedAttribute : Attribute
{
    public int Priority { get; set; }
}