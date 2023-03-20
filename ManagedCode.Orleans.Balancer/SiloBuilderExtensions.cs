using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Orleans.Balancer;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder UseOrleansBalancer(this ISiloBuilder siloBuilder)
    {
        return UseOrleansBalancer(siloBuilder, _ => { });
    }

    public static ISiloBuilder UseOrleansBalancer(this ISiloBuilder siloBuilder, Action<OrleansBalancerOptions> options)
    {
        siloBuilder.AddStartupTask<BalancerStartupTask>();

        siloBuilder.ConfigureServices(serviceCollection =>
        {
            serviceCollection.AddOptions<OrleansBalancerOptions>()
                //.Bind(context.Configuration.GetSection("ActivationShedding"))
                // ReSharper disable once ConvertClosureToMethodGroup
                .PostConfigure(sheddingOptions => options(sheddingOptions));
            // .ValidateDataAnnotations();
        });

        return siloBuilder;
    }
}