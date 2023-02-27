using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.AmazonS3.Tests;

public class AmazonS3SubscriptionStorageFactory : ISubscriptionStorageFactory
{
    public ISubscriptionStorage Create()
    {
        return CreateInstance();
    }

    public void Cleanup()
    {
        var connectionInfo = AmazonS3ConnectionInfoUtil.ConnectionInfo.Value;
        new CleanupUtil(connectionInfo).Cleanup();
    }

    private AmazonS3SubscriptionsStorage CreateInstance()
    {
        var connectionInfo = AmazonS3ConnectionInfoUtil.ConnectionInfo.Value;
        return new AmazonS3SubscriptionsStorage(connectionInfo.Credentials, connectionInfo.Config, connectionInfo.Options);
    }
}