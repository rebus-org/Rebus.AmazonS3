using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Rebus.AmazonS3.AmazonS3;
using Rebus.Logging;
using Rebus.Subscriptions;
using System;

namespace Rebus.Config
{
    /// <summary>
    /// Provides extensions methods for configuring the Amazon S3 subscription storage
    /// </summary>
    public static class AmazonS3SubscriptionsConfigurationExtensions
    {
        /// <summary>
        /// Configures the storage of subscriptions in Amazon S3
        /// </summary>
        public static void StoreInAmazonS3(this StandardConfigurer<ISubscriptionStorage> configurer, Func<IAmazonS3> amazonS3Factory)
        {
            if (amazonS3Factory == null) throw new ArgumentNullException(nameof(amazonS3Factory));

            Configure(configurer, amazonS3Factory);
        }

        /// <summary>
        /// Configures the storage of subscriptions in Amazon S3
        /// </summary>
        public static void StoreInAmazonS3(this StandardConfigurer<ISubscriptionStorage> configurer,
           AWSCredentials credentials, AmazonS3Config config)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Configure(configurer, credentials, config);
        }

        /// <summary>
        /// Configures the storage of subscriptions in Amazon S3
        /// </summary>
        public static void StoreInAmazonS3(this StandardConfigurer<ISubscriptionStorage> configurer, string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint, string bucketName, TransferUtilityConfig transferUtilityConfig = null)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (accessKeyId == null) throw new ArgumentNullException(nameof(accessKeyId));
            if (secretAccessKey == null) throw new ArgumentNullException(nameof(secretAccessKey));
            if (regionEndpoint == null) throw new ArgumentNullException(nameof(regionEndpoint));

            Configure(configurer, new BasicAWSCredentials(accessKeyId, secretAccessKey), new AmazonS3Config { RegionEndpoint = regionEndpoint });
        }

        private static void Configure(StandardConfigurer<ISubscriptionStorage> configurer, Func<IAmazonS3> amazonS3Factory)
        {
            configurer.Register(c => new AmazonS3SubscriptionsStorage(amazonS3Factory, c.Get<IRebusLoggerFactory>()));
        }

        private static void Configure(StandardConfigurer<ISubscriptionStorage> configurer, AWSCredentials credentials, AmazonS3Config config)
        {
            configurer.Register(c => new AmazonS3SubscriptionsStorage(credentials, config, c.Get<IRebusLoggerFactory>()));
        }
    }
}
