using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            services.Configure<ApplicationConfiguration>(Configuration.GetSection("Kinesis"));
            base.ConfigureServices(services);
            return services.AddLocalStack();
        }

        protected override IConfigurationBuilder ConfigureConfiguration(IConfigurationBuilder builder)
        {
            return builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Logging:LogLevel:Default", "Debug"},
                {"Kinesis:StreamArn", "reader-stream"}
            });
        }

        [Fact]
        public async Task Test1()
        {
            var reader = await Subject.CreateReaderAsync(new ShardRef("shardId-000000000003", null),
                    new ShardPosition("49599642058112567479285019334419276443952743659029397554"),
                    CancellationToken.None)
                .ConfigureAwait(false);

            do
            {
                await reader.ReadNextAsync().ConfigureAwait(false);
                output.WriteLine(JsonConvert.SerializeObject(new
                    {reader.EndOfShard, reader.MillisBehindLatest, reader.Records.Count}));
            } while (!reader.EndOfShard);

        }
    }
}
