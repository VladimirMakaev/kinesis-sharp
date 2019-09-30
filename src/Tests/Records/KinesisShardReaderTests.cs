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
             *"AWS_ACCESS_KEY_ID": "ASIA56ZSXAX7XAP3YSZP",
        "AWS_SECRET_ACCESS_KEY": "hbbM/J465ZpvEf/8S6bC0SChwTvF4JN0r1P0D40J",
        "AWS_SESSION_TOKEN": "FQoGZXIvYXdzEOn//////////wEaDImSc8tHXFFK2YgSNSKRAkhDSAXhSX1IxwbZltXtT2EjzGS6uvx1hS4Q5/d1lTpXxl6NlrTwthsA/EbLW5kIE+W40mHEz+w3PfaHtor2X+piPGlzBV+T4XQkDSseSYg+mQk/KkxL3N8kiSfWcsfKw0gA3KGGs5vxKjFwQRcGO5pNFz0JJ11wGqalhCdRMxqjfKR99jUWFOxRDh8KveIUHgQebAexXPNasqPIMAMcJZWbnr9rqdpL3Oi4fEYPXrrsoBqzav+IxrjN4+GzgG/dqKnC6zqP9R3q3nefvU6kyOv33YH4KOfgkBanY6+glkcqrahw+J1ZrJXaVYScUedh6nJxkMeeeV1JKcN4/hX/y5ZzQyo7TT6m7FBiBco2AOANjSirhMXsBQ==",
        
             *
             *
             */

            services.Configure<ApplicationConfiguration>(Configuration.GetSection("Kinesis"));
            services.AddSingleton<IAmazonKinesis>(p =>

                new AmazonKinesisClient(new SessionAWSCredentials("ASIA56ZSXAX7XAP3YSZP", "hbbM/J465ZpvEf/8S6bC0SChwTvF4JN0r1P0D40J",
                    "FQoGZXIvYXdzEOn//////////wEaDImSc8tHXFFK2YgSNSKRAkhDSAXhSX1IxwbZltXtT2EjzGS6uvx1hS4Q5/d1lTpXxl6NlrTwthsA/EbLW5kIE+W40mHEz+w3PfaHtor2X+piPGlzBV+T4XQkDSseSYg+mQk/KkxL3N8kiSfWcsfKw0gA3KGGs5vxKjFwQRcGO5pNFz0JJ11wGqalhCdRMxqjfKR99jUWFOxRDh8KveIUHgQebAexXPNasqPIMAMcJZWbnr9rqdpL3Oi4fEYPXrrsoBqzav+IxrjN4+GzgG/dqKnC6zqP9R3q3nefvU6kyOv33YH4KOfgkBanY6+glkcqrahw+J1ZrJXaVYScUedh6nJxkMeeeV1JKcN4/hX/y5ZzQyo7TT6m7FBiBco2AOANjSirhMXsBQ=="), RegionEndpoint.EUWest1));
            services.AddSingleton<IKinesisShardReaderFactory, KinesisShardReaderFactory>();
            return base.ConfigureServices(services);
            //return services.AddLocalStack();
        }

        protected override IConfigurationBuilder ConfigureConfiguration(IConfigurationBuilder builder)
        {
            return builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Logging:LogLevel:Default", "Debug"},
                { "Kinesis:StreamArn", "shard-test-1"}
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
            } while (!reader.EndOfShard);
        }
    }
}
