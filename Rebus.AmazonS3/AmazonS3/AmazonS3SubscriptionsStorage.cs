using System.Threading.Tasks;
using Rebus.Subscriptions;
using Amazon.Runtime;
using Amazon.S3;
using Rebus.Logging;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3.Model;
using System;
using System.Net;
using Rebus.Exceptions;
using Amazon.S3.Util;
using Rebus.Config;
using System.Threading;

namespace Rebus.AmazonS3.AmazonS3
{
    /// <summary>
    /// Implementation of <see cref="ISubscriptionStorage"/> that stores subscriptions in Amazon S3
    /// </summary>
    internal class AmazonS3SubscriptionsStorage : ISubscriptionStorage
    {
        private readonly AWSCredentials _awsCredentials;
        private readonly AmazonS3Config _awsConfig;
        private readonly AmazonS3DataBusOptions _options;
        private readonly Func<IAmazonS3> _amazonS3Factory;
        private readonly ILog _log;

        public AmazonS3SubscriptionsStorage(AWSCredentials credentials, AmazonS3Config amazonS3Config, AmazonS3DataBusOptions options, IRebusLoggerFactory rebusLoggerFactory)
            : this(rebusLoggerFactory)
        {
            _awsCredentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _awsConfig = amazonS3Config ?? throw new ArgumentNullException(nameof(amazonS3Config));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public AmazonS3SubscriptionsStorage(Func<IAmazonS3> amazonS3Factory, AmazonS3DataBusOptions options, IRebusLoggerFactory rebusLoggerFactory)
            : this(rebusLoggerFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _amazonS3Factory = amazonS3Factory;
        }

        private AmazonS3SubscriptionsStorage(IRebusLoggerFactory rebusLoggerFactory)
        {
            _log = rebusLoggerFactory?.GetLogger<AmazonS3SubscriptionsStorage>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
        }

        public bool IsCentralized
        {
            get { return true; }
        }

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            await EnsureBucketExistAsync();

            var subscriptions = await GetSubscriptions(topic);

            return subscriptions.ToArray();
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            await EnsureBucketExistAsync();

            string key = $"{topic}/{subscriberAddress}";

            await PutObjectAsync(key);
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            await EnsureBucketExistAsync();

            string key = $"{topic}/{subscriberAddress}";

            await DeleteObjectAsync(key);
        }

        private async Task<IList<string>> GetSubscriptions(string topic)
        {
            var topicKeys = await GetKeysForPrefix(topic);

            var keys = topicKeys.Select(k => k.Substring(topic.Length + 1));

            return keys.ToList();
        }

        private async Task<IList<string>> GetKeysForPrefix(string prefix)
        {
            var keys = new List<string>();

            var request = new ListObjectsRequest
            {
                BucketName = _options.BucketName,
                Prefix = prefix
            };

            using (var client = CreateS3Client())
            {
                ListObjectsResponse response = await client.ListObjectsAsync(request);

                foreach (S3Object obj in response.S3Objects)
                {
                    keys.Add(obj.Key);
                }
            }

            return keys;
        }

        private async Task<bool> ObjectExistsAsync(string key)
        {
            using (IAmazonS3 s3Client = CreateS3Client())
            {
                try
                {
                    await s3Client.GetObjectMetadataAsync(_options.BucketName, key);
                    return true;
                }
                catch (AmazonS3Exception e)
                {
                    if (e.StatusCode == HttpStatusCode.NotFound)
                    {
                        return false;
                    }

                    throw new RebusApplicationException(e, "Unexpected exception occured");
                }
                catch (Exception e)
                {
                    throw new RebusApplicationException(e, "Unexpected exception occured");
                }
            }
        }

        private async Task EnsureBucketExistAsync()
        {
            using (IAmazonS3 s3Client = CreateS3Client())
            {
                try
                {
                    if (await AmazonS3Util.DoesS3BucketExistAsync(s3Client, _options.BucketName))
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    throw new RebusApplicationException(e, "Unexpected exception occured");
                }

                try
                {
                    await s3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = _options.BucketName,
                        UseClientRegion = true
                    });
                }
                catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.Conflict)
                {
                }
            }
        }

        private async Task PutObjectAsync(string key)
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };
            using (var client = CreateS3Client())
            {
                try
                {
                    await client.PutObjectAsync(request);
                }
                catch (Exception e)
                {
                    throw new RebusApplicationException(e, "Unexpected exception occured");
                }
            }
        }

        private async Task DeleteObjectAsync(string key)
        {
            if (await ObjectExistsAsync(key))
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = key
                };
                using (var client = CreateS3Client())
                {
                    try
                    {
                        await client.DeleteObjectAsync(request);
                    }
                    catch (Exception e)
                    {
                        throw new RebusApplicationException(e, "Unexpected exception occured");
                    }
                }
            }
        }

        private IAmazonS3 CreateS3Client()
        {
            if (_amazonS3Factory != null)
                return _amazonS3Factory();

            return new AmazonS3Client(_awsCredentials, _awsConfig);
        }
    }
}
