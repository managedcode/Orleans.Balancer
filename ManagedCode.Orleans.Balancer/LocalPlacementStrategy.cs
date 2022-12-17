using Orleans.Runtime;

namespace ManagedCode.Orleans.Balancer;

[Serializable]
[GenerateSerializer]
internal class LocalPlacementStrategy : PlacementStrategy
{
}