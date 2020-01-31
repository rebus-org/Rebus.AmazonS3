using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.AmazonS3.Tests
{
    [TestFixture]
    [Ignore("just some code to post on GitHub")]
    public class AmazonS3CodeExample : FixtureBase
    {
        [Test]
        public async Task ThisIsHowItLooks()
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "api-tjek"))
                .DataBus(d =>
                {
                    var options = new AmazonS3DataBusOptions("my-bucket")
                    {
                        DoNotUpdateLastReadTime = true
                    };

                    var credentials = new BasicAWSCredentials("access-key", "secret-key");
                    var config = new AmazonS3Config();

                    d.StoreInAmazonS3(credentials, config, options);
                })
                .Start();
        }
    }
}