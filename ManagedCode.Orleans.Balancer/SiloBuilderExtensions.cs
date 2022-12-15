using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Orleans.Balancer;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder UseOrleansBalancer(this ISiloBuilder siloBuilder)
    {
        siloBuilder.AddPlacementDirector<LocalPlacementStrategy, LocalPlacementDirector>();
        return UseOrleansBalancer(siloBuilder, _ => { });
    }

    public static ISiloBuilder UseOrleansBalancer(this ISiloBuilder siloBuilder, Action<OrleansBalancerOptions> options)
    {
        siloBuilder.ConfigureServices(serviceCollection =>
        {
            // collection.AddSingleton<PlacementStrategy, MyPlacementStrategy>();

            serviceCollection.AddOptions<OrleansBalancerOptions>()
                //.Bind(context.Configuration.GetSection("ActivationShedding"))
                // ReSharper disable once ConvertClosureToMethodGroup
                .PostConfigure(sheddingOptions => options(sheddingOptions));
            // .ValidateDataAnnotations();
        });

        // siloBuilder.AddStartupTask<GrainBalancer>();

        return siloBuilder;
    }
}