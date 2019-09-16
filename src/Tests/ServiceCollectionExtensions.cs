using Amazon.Kinesis;
using LocalStack.Client.Contracts;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Tests
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalStack(this IServiceCollection services)
        {
            services.AddSingleton(ContainerBasedFixture<IAmazonKinesis>.CreateSession());
            return services.AddSingleton<IAmazonKinesis, AmazonKinesisClient>(p =>
                p.GetRequiredService<ISession>().CreateClient<AmazonKinesisClient>());
        }
    }
}