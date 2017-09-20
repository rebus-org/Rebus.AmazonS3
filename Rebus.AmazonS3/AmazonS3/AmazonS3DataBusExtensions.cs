using System;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.Logging;

namespace Rebus.AmazonS3
{
    /// <summary>
    /// Provides extensions methods for configuring the Amazon S3 storage for the data bus
    /// </summary>
    public static class AmazonS3DataBusExtensions
    {
        /// <summary>
        /// Configures the data bus to store data in Amazon S3
        /// </summary>
        public static void StoreInAmazonS3(this StandardConfigurer<IDataBusStorage> configurer, AWSCredentials credentials, AmazonS3Config config, AmazonS3DataBusOptions options, TransferUtilityConfig transferUtilityConfig = null)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Configure(configurer, credentials, config, options, transferUtilityConfig ?? new TransferUtilityConfig());
        }

        /// <summary>
        /// Configures the data bus to store data in Amazon S3
        /// </summary>
        public static void StoreInAmazonS3(this StandardConfigurer<IDataBusStorage> configurer, string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint, string bucketName, TransferUtilityConfig transferUtilityConfig = null)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (accessKeyId == null) throw new ArgumentNullException(nameof(accessKeyId));
            if (secretAccessKey == null) throw new ArgumentNullException(nameof(secretAccessKey));
            if (regionEndpoint == null) throw new ArgumentNullException(nameof(regionEndpoint));
            if (bucketName == null) throw new ArgumentNullException(nameof(bucketName));


            Configure(
                configurer, 
                new BasicAWSCredentials(accessKeyId, secretAccessKey),
                new AmazonS3Config { RegionEndpoint = regionEndpoint }, 
                new AmazonS3DataBusOptions(bucketName),
                transferUtilityConfig ?? new TransferUtilityConfig());
        }

        private static void Configure(StandardConfigurer<IDataBusStorage> configurer, AWSCredentials credentials, AmazonS3Config config, AmazonS3DataBusOptions options, TransferUtilityConfig transferUtilityConfig)
        {
            configurer.Register(c => new AmazonS3DataBusStorage(credentials, config, options, transferUtilityConfig, c.Get<IRebusLoggerFactory>()));
        }
    }
}