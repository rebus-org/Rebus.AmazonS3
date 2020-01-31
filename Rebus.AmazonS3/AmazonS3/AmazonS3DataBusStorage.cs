using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Rebus.AmazonS3.Core;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Time;
using Amazon.S3.Util;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Rebus.AmazonS3.Tests")]

namespace Rebus.AmazonS3
{
    /// <summary>
    /// Implementation of <see cref="IDataBusStorage"/> that stores data in Amazon S3
    /// </summary>
    class AmazonS3DataBusStorage : IDataBusStorage
    {
        readonly AWSCredentials _credentials;
        readonly AmazonS3Config _amazonS3Config;
        readonly TransferUtilityConfig _transferUtilityConfig;
        readonly IRebusTime _rebusTime;
        readonly AmazonS3DataBusOptions _options;
        readonly S3MetadataCollectionFactory _metadataCollectionFactory;
        readonly ILog _log;

        public AmazonS3DataBusStorage(
            AWSCredentials credentials, 
            AmazonS3Config amazonS3Config, 
            AmazonS3DataBusOptions options, 
            TransferUtilityConfig transferUtilityConfig, 
            IRebusLoggerFactory rebusLoggerFactory,
            IRebusTime rebusTime)
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _amazonS3Config = amazonS3Config ?? throw new ArgumentNullException(nameof(amazonS3Config));
            _transferUtilityConfig = transferUtilityConfig ?? throw new ArgumentNullException(nameof(transferUtilityConfig));
            _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _log = rebusLoggerFactory?.GetLogger<AmazonS3DataBusStorage>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _metadataCollectionFactory = new S3MetadataCollectionFactory(options);

            if (options.AutoCreateBucket)
            {
                EnsureBucketExistsAsync().GetAwaiter().GetResult();
            }
        }

        public async Task Save(string id, Stream source, Dictionary<string, string> metadata = null)
        {
            await TryCatchAsync(id, async (identity) =>
            {
                using (var transferUtility = CreateTransferUtility())
                {
                    var metadataCollection = _metadataCollectionFactory.Create();
                    metadataCollection.AddRange(metadata);
                    metadataCollection[MetadataKeys.SaveTime] = _rebusTime.Now.ToString("O");

                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = source,
                        BucketName = _options.BucketName,
                        Key = identity.Key
                    };
                    metadataCollection.SaveTo(uploadRequest.Metadata);

                    await transferUtility.UploadAsync(uploadRequest);
                }

                return true;
            });
        }

        public async Task<Stream> Read(string id)
        {
            return await TryCatchAsync(id, async (identity) =>
            {
                using (var s3Client = CreateS3Client())
                {
                    await UpdateReadTimeAsync(identity, s3Client);

                    // Open stream to object
                    using (var transferUtility = CreateTransferUtility(s3Client))
                    {
                        return await transferUtility.OpenStreamAsync(new TransferUtilityOpenStreamRequest
                        {
                            BucketName = _options.BucketName,
                            Key = identity.Key
                        });
                    }
                }
            });
        }

        public async Task<Dictionary<string, string>> ReadMetadata(string id)
        {
            return await TryCatchAsync(id, async (identity) =>
            {
                using (var s3Client = CreateS3Client())
                {
                    return (await GetObjectMetadataAsync(s3Client, identity, true)).ToDictionary();
                }
            });
        }

        async Task EnsureBucketExistsAsync()
        {
            try
            {
                using (var s3Client = CreateS3Client())
                {
                    if (await AmazonS3Util.DoesS3BucketExistAsync(s3Client, _options.BucketName)) return;

                    _log.Info("Creating bucket '{BucketName}'", _options.BucketName);
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
                        _log.Info("Bucket '{BucketName}' already existed");
                    }
                }
            }
            catch (AmazonS3Exception e) when (e.ErrorCode.Equals("InvalidAccessKeyId") || e.ErrorCode.Equals("InvalidSecurity"))
            {
                throw new RebusApplicationException(e, "Invalid AWS credentials");
            }
            catch (AmazonS3Exception e)
            {
                throw new RebusApplicationException(e, "Unexpected Amazon S3 exception occurred");
            }
            catch (Exception e)
            {
                throw new RebusApplicationException(e, "Unexpected exception");
            }
        }

        async Task UpdateReadTimeAsync(ObjectIdentity identity, IAmazonS3 s3Client)
        {
            if (_options.DoNotUpdateLastReadTime) return;

            var metadataCollection = await GetObjectMetadataAsync(s3Client, identity, false);
            metadataCollection[MetadataKeys.ReadTime] = _rebusTime.Now.ToString("O");
            var copyObjectRequest = new CopyObjectRequest
            {
                SourceBucket = _options.BucketName,
                DestinationBucket = _options.BucketName,
                SourceKey = identity.Key,
                DestinationKey = identity.Key,
                MetadataDirective = S3MetadataDirective.REPLACE
            };
            metadataCollection.SaveTo(copyObjectRequest.Metadata);
            await s3Client.CopyObjectAsync(copyObjectRequest);
        }

        async Task<S3MetadataCollection> GetObjectMetadataAsync(IAmazonS3 s3Client, ObjectIdentity identity, bool addContentLength)
        {
            var metadataResponse = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = identity.Key
            });

            var metadataCollection = _metadataCollectionFactory.Create();
            metadataCollection.LoadFrom(metadataResponse.Metadata);

            if (addContentLength)
            {
                metadataCollection[MetadataKeys.Length] = metadataResponse.ContentLength.ToString();
            }
            
            return metadataCollection;
        }

        AmazonS3Client CreateS3Client()
        {
            return new AmazonS3Client(_credentials, _amazonS3Config);
        }

        ITransferUtility CreateTransferUtility(AmazonS3Client s3Client = null)
        {
            if (s3Client == null)
            {
                s3Client = CreateS3Client();
            }
            return new TransferUtility(s3Client, _transferUtilityConfig);
        }

        async Task<T> TryCatchAsync<T>(string id, Func<ObjectIdentity,Task<T>> asyncFunc)
        {
            try
            {
                return await asyncFunc(new ObjectIdentity(id, _options));
            }
            catch (AmazonS3Exception e) when (e.ErrorCode.Equals("InvalidAccessKeyId") || e.ErrorCode.Equals("InvalidSecurity"))
            {
                throw new RebusApplicationException(e, "Invalid AWS credentials");
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // Rebus expects an ArgumentException when an unknown ID is provided
                throw new ArgumentException($"Could not find data ID {id}", e);
            }
            catch (AmazonS3Exception e)
            {
                throw new RebusApplicationException(e, "Unexpected Amazon S3 exception occurred");
            }
            catch (Exception e)
            {
                throw new RebusApplicationException(e, "Unexpected exception");
            }
        }
    }
}
