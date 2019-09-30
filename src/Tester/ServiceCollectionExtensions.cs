using Amazon.Kinesis;
using LocalStack.Client;
using LocalStack.Client.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Tester
{
    public static class ServiceCollectionExtensions
    {
        private static ISession CreateLocalStackSession()
        {
            var awsAccessKeyId = "Key LockId";
            var awsAccessKey = "Secret Key";
            var awsSessionToken = "Token";
            var regionName = "us-west-1";
            var localStackHost = "localhost";

            var session = SessionStandalone
                .Init()
                .WithSessionOptions(awsAccessKeyId, awsAccessKey, awsSessionToken, regionName)
                .WithConfig(localStackHost)
                .Create();
            return session;
        }

        public static void AddLocalStack(this IServiceCollection services)
        {
            var session = CreateLocalStackSession();
            services.AddSingleton<IAmazonKinesis>(session.CreateClient<AmazonKinesisClient>());
        }
    }
}
