using Rebus.AmazonS3.AmazonS3;
using Rebus.Logging;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.AmazonS3.Tests
{
    public class AmazonS3SubscriptionStorageFactory : ISubscriptionStorageFactory
    {
        public ISubscriptionStorage Create()
        {
            var connectionInfo = AmazonS3ConnectionInfoUtil.ConnectionInfo.Value;
            return new AmazonS3SubscriptionsStorage(connectionInfo.Credentials, connectionInfo.Config, new ConsoleLoggerFactory(false));
        }

        public void Cleanup()
        {
        }
    }
}
