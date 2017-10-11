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
            var consoleLoggerFactory = new ConsoleLoggerFactory(true);
            var storage = new AmazonS3SubscriptionsStorage(null, consoleLoggerFactory);

            //storage.EnsureTableIsCreated();

            return storage;
        }

        public void Cleanup()
        {
            //SqlTestHelper.DropTable(TableName);
        }
    }
}
