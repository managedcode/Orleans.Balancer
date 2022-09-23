using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.Balancer;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder UseActivationShedding(this ISiloBuilder siloBuilder)
    {
        return UseActivationShedding(siloBuilder, _ => { });
    }

    public static ISiloBuilder UseActivationShedding(this ISiloBuilder siloBuilder, Action<ActivationSheddingOptions> options)
    {
        siloBuilder.ConfigureServices(((context, collection) =>
        {
            collection.AddOptions<ActivationSheddingOptions>()
                .Bind(context.Configuration.GetSection("ActivationShedding"))
                // ReSharper disable once ConvertClosureToMethodGroup
                .PostConfigure(sheddingOptions => options(sheddingOptions));
            // .ValidateDataAnnotations();
        }));

        siloBuilder.AddIncomingGrainCallFilter<ActivationSheddingFilter>();

        return siloBuilder;
    }
}