using System;
using System.Linq;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Rebus.Config;

namespace Rebus.AmazonS3.Tests
{
    public class ConnectionInfo
    {
        private ConnectionInfo(string accessKeyId, string secretAccessKey, string regionEndpointName, string bucketName)
        {
            AccessKeyId = accessKeyId;
            SecretAccessKey = secretAccessKey;
            RegionEndpoint = GetRegionEndpoint(regionEndpointName);
            BucketName = bucketName;
        }

        public string AccessKeyId { get; }
        public string SecretAccessKey { get; }
        public RegionEndpoint RegionEndpoint { get; }
        public string BucketName { get; }

        public AWSCredentials Credentials 
            => new BasicAWSCredentials(AccessKeyId, SecretAccessKey);

        public AmazonS3Config Config 
            => new AmazonS3Config {RegionEndpoint = RegionEndpoint};

        public AmazonS3DataBusOptions Options
            => new AmazonS3DataBusOptions(BucketName);

        public static ConnectionInfo CreateFromString(string textString)
        {
            Console.WriteLine("Parsing connectionInfo from string: {0}", textString);

            var keyValuePairs = textString.Split("; ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            try
            {
                var keysAndValues = keyValuePairs
                    .Select(kvp => kvp.Split('='))
                    .ToDictionary(kv => kv.First(), kv => kv.Last());

                return new ConnectionInfo(
                    keysAndValues["AccessKeyId"],
                    keysAndValues["SecretAccessKey"],
                    keysAndValues["RegionEndpoint"],
                    keysAndValues["BucketName"]
                );
            }
            catch (Exception exception)
            {
                throw new FormatException(
                    "Could not extract access key ID, secret access key, and region endpoint from string - expected the form \'AccessKeyId=blabla; SecretAccessKey=blablalba; RegionEndpoint=something\'",
                    exception);
            }
        }

        private static RegionEndpoint GetRegionEndpoint(string regionEndpointName)
        {
            try
            {
                return RegionEndpoint.GetBySystemName(regionEndpointName);
            }
            catch (Exception exception)
            {
                throw new FormatException($"The region endpoint '{regionEndpointName}' could not be recognized",
                    exception);
            }
        }
    }
}