using NUnit.Framework;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.AmazonS3.Tests;

[TestFixture, Category("amazons3")]
public class AmazonS3SubscriptionBasicSubscriptionOperations : BasicSubscriptionOperations<AmazonS3SubscriptionStorageFactory>
{
}