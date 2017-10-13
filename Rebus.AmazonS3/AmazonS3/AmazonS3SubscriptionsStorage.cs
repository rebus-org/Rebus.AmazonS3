using System.Threading.Tasks;
using Rebus.Subscriptions;
using Amazon.Runtime;
using Amazon.S3;
using Rebus.Logging;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Rebus.Extensions;
using Amazon.S3.Model;
using System.IO;
using System;
using System.Net;
using Rebus.Exceptions;
using Amazon.S3.Util;
using Rebus.Config;

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

        public AmazonS3SubscriptionsStorage(Func<IAmazonS3> amazonS3Factory, IRebusLoggerFactory rebusLoggerFactory)
            : this(rebusLoggerFactory)
        {
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
            await EnsureBucketExist();

            var subscriptions = await GetSubscriptions(topic);

            HashSet<string> subscribers;
            if(subscriptions.TryGetValue(topic, out subscribers))
                return subscribers.ToArray();

            return new string[0];
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            await EnsureBucketExist();

            var subscriptions = await GetSubscriptions(topic);

            subscriptions
                .GetOrAdd(topic, () => new HashSet<string>())
                .Add(subscriberAddress);

            await SaveToS3(subscriptions, topic);
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            await EnsureBucketExist();

            var subscriptions = await GetSubscriptions(topic);

            subscriptions
                .GetOrAdd(topic, () => new HashSet<string>())
                .Remove(subscriberAddress);

            await SaveToS3(subscriptions, topic);
        }

        private async Task<Dictionary<string, HashSet<string>>> GetSubscriptions(string topic)
        {
            if (await ObjectExists(topic))
            {
                string jsonText = await FetchFromS3(topic);

                var subscriptions = JsonConvert.DeserializeObject<Dictionary<string, HashSet<string>>>(jsonText);

                return subscriptions;
            }

            return new Dictionary<string, HashSet<string>>();
        }

        private async Task<bool> ObjectExists(string key)
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

        private async Task<string> FetchFromS3(string key)
        {
            var request = new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            using (IAmazonS3 s3Client = CreateS3Client())
            {
                try
                {
                    using (var response = await s3Client.GetObjectAsync(request))
                    {
                        using (StreamReader reader = new StreamReader(response.ResponseStream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
                catch (AmazonS3Exception e)
                {
                    if (e.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new ArgumentException($"Could not locate an object with key {key} in bucket: {_options.BucketName}", e);
                    }

                    throw new RebusApplicationException(e, "Unexpected exception occured");
                }
                catch (Exception e)
                {
                    throw new RebusApplicationException(e, "Unexpected exception occured");
                }
            }
        }

        private async Task EnsureBucketExist()
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

        private async Task SaveToS3<T>(T data, string key)
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };
            request.ContentType = "application/json";
            request.ContentBody = JsonConvert.SerializeObject(data);

            using (IAmazonS3 s3Client = CreateS3Client())
            {
                try
                {
                    await s3Client.PutObjectAsync(request);
                }
                catch (Exception e)
                {
                    throw new RebusApplicationException(e, "Unexpected exception occured");
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
