using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Kinesis;
using Amazon.Runtime;
using KinesisSharp;
using KinesisSharp.Configuration;
using KinesisSharp.Records;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Records
{
    public class KinesisShardReaderTests : ContainerBasedFixture<KinesisShardReaderFactory>
    {
        public KinesisShardReaderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private readonly ITestOutputHelper output;

        protected override IServiceCollection ConfigureServices(IServiceCollection services)
        {
            /*
             *@{AccessKey=ASIA56ZSXAX7Z5WLXAI3; SecretKey=M169U9XkT0UDu6XU50vOfuMSxnjoErB/JCra6Pb7; Token=FQoGZXIvYXdzEB4aDIUgzRRz6ifbwrvBGSKRAtGIps+4tJRDxTYAGynVVkRqNRl93Uj7yN0wXaWxwtLKqQSWHKEUy+tcBjviNNss1kxNXY171e0FGumKr13ke8FoXIN0MFQSsdaXmeSZ1B390uRmsXcfPIofi2GtlYaV4Hw1sH6/M4yLGcoTVWMohPt+eQa1oIJrdgUrS0mTruLuupqNNbnZB/gIA0OrWsxDI+93ZSj/Jhj7OOFxfu5ndaD8HBldLBac9z9gKjSOkhHiLJo3MfhdggSrZ7I7e9aBCNoWvKb7JnQOOdE3xBulI1rTxhud/b9dVlfBE85EvV6BGdN9cTGbzbg5LDdXiij/iLWS0jGysSVIVUvDnn6HtNe/OBo6RWnWvggE/yc4V4G45yjo8YjtBQ==}
             *
             *
             *
             *
             */

            var cred = new
            {
                AccessKey = "ASIA56ZSXAX7Z5WLXAI3",
                SecretKey = "M169U9XkT0UDu6XU50vOfuMSxnjoErB/JCra6Pb7",
                Token =
                    "FQoGZXIvYXdzEB4aDIUgzRRz6ifbwrvBGSKRAtGIps+4tJRDxTYAGynVVkRqNRl93Uj7yN0wXaWxwtLKqQSWHKEUy+tcBjviNNss1kxNXY171e0FGumKr13ke8FoXIN0MFQSsdaXmeSZ1B390uRmsXcfPIofi2GtlYaV4Hw1sH6/M4yLGcoTVWMohPt+eQa1oIJrdgUrS0mTruLuupqNNbnZB/gIA0OrWsxDI+93ZSj/Jhj7OOFxfu5ndaD8HBldLBac9z9gKjSOkhHiLJo3MfhdggSrZ7I7e9aBCNoWvKb7JnQOOdE3xBulI1rTxhud/b9dVlfBE85EvV6BGdN9cTGbzbg5LDdXiij/iLWS0jGysSVIVUvDnn6HtNe/OBo6RWnWvggE/yc4V4G45yjo8YjtBQ=="
            };


            services.Configure<ApplicationConfiguration>(Configuration.GetSection("Kinesis"));
            services.AddSingleton<IAmazonKinesis>(p =>
                new AmazonKinesisClient(
                    new SessionAWSCredentials(
                        cred.AccessKey,
                        cred.SecretKey,
                        cred.Token),
                    RegionEndpoint.EUWest1));
            services.AddSingleton<IKinesisShardReaderFactory, KinesisShardReaderFactory>();
            return base.ConfigureServices(services);
            //return services.AddLocalStack();
        }

        protected override IConfigurationBuilder ConfigureConfiguration(IConfigurationBuilder builder)
        {
            return builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Logging:LogLevel:Default", "Debug"}, {"Kinesis:StreamArn", "vladimir-stream-1"}
            }).AddEnvironmentVariables();
        }

        [Fact]
        public async Task Test1()
        {
            var reader = await Subject.CreateReaderAsync("shardId-000000000001", ShardPosition.TrimHorizon);

            do
            {
                await reader.ReadNextAsync().ConfigureAwait(false);
                output.WriteLine(JsonConvert.SerializeObject(new
                {
                    reader.EndOfShard, reader.MillisBehindLatest, reader.Records.Count
                }));
            } while (!reader.EndOfShard && reader.MillisBehindLatest > 0);
        }
    }
}
