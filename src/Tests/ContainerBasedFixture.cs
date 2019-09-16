using System;
using LocalStack.Client;
using LocalStack.Client.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Tests
{
    public class ContainerBasedFixture<TSubject> where TSubject : class
    {
        private readonly Lazy<IServiceProvider> provider;

        public ContainerBasedFixture()
        {
            provider = new Lazy<IServiceProvider>(CreateProvider);
        }

        public TSubject Subject => provider.Value.GetRequiredService<TSubject>();

        private IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            RegisterSubject(services);
            return services.BuildServiceProvider();
        }

        protected T Dependency<T>()
        {
            return provider.Value.GetRequiredService<T>();
        }


        protected virtual IServiceCollection ConfigureServices(IServiceCollection services)
        {
            return services;
        }

        protected virtual IServiceCollection RegisterSubject(IServiceCollection services)
        {
            return services.AddSingleton<TSubject>();
        }

        public static ISession CreateSession()
        {
            var awsAccessKeyId = "Key Id";
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
    }
}