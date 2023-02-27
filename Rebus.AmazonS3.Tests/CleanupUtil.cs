using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.AmazonS3.Tests;

public class CleanupUtil
{
    private readonly ConnectionInfo _connectionInfo;

    public CleanupUtil(ConnectionInfo connectionInfo)
    {
        _connectionInfo = connectionInfo;
    }

    public void Cleanup()
    {
        using (var client = CreateS3Client())
        {
            IList<string> keys = GetObjectKeys(client);

            DeleteObjects(client, keys);
        }
    }

    private List<string> GetObjectKeys(IAmazonS3 client)
    {
        var keys = new List<string>();
        var listRequest = new ListObjectsRequest
        {
            BucketName = _connectionInfo.BucketName
        };

        var response = AsyncHelper.RunSync(() =>
        {
            return client.ListObjectsAsync(listRequest);
        });

        foreach (S3Object obj in response.S3Objects)
        {
            keys.Add(obj.Key);
        }

        return keys;
    }

    private void DeleteObjects(IAmazonS3 client, IList<string> keys)
    {
        var objects = new List<KeyVersion>();
        foreach (string key in keys)
        {
            objects.Add(new KeyVersion { Key = key });
        }

        if (objects.Count > 0)
        {
            var request = new DeleteObjectsRequest
            {
                BucketName = _connectionInfo.BucketName,
                Objects = objects
            };

            AsyncHelper.RunSync(() =>
            {
                return client.DeleteObjectsAsync(request);
            });
        }
    }

    private IAmazonS3 CreateS3Client()
    {
        return new AmazonS3Client(_connectionInfo.Credentials, _connectionInfo.Config);
    }

    private static class AsyncHelper
    {
        private static readonly TaskFactory _myTaskFactory = new
            TaskFactory(CancellationToken.None,
                TaskCreationOptions.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return _myTaskFactory.StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            _myTaskFactory.StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
    }
}