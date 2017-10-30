using Rebus.Exceptions;
using System;
using System.IO;

namespace Rebus.AmazonS3.Tests
{
    public class AmazonS3ConnectionInfoUtil
    {
        const string ConnectionInfoFile = "s3_connectioninfo.txt";
        const string ConnectionInfoEnvironment = "REBUS_S3_CONNECTIONINFO";

        public static readonly Lazy<ConnectionInfo> ConnectionInfo = new Lazy<ConnectionInfo>(() =>
        {
            // Load from file
            var file = GetFilePath(ConnectionInfoFile);
            if (File.Exists(file))
            {
                try
                {
                    return Tests.ConnectionInfo.CreateFromString(File.ReadAllText(file));
                }
                catch (Exception exception)
                {
                    throw new RebusConfigurationException(exception, $"Could not get connection string information from file {file}");
                }
            }

            // Load from environment
            var env = Environment.GetEnvironmentVariable(ConnectionInfoEnvironment);
            if (env != null)
            {
                try
                {
                    return Tests.ConnectionInfo.CreateFromString(env);
                }
                catch (Exception exception)
                {
                    throw new RebusConfigurationException(exception, "Could not get connection string information");
                }
            }

            throw new RebusConfigurationException("Missing Amazon S3 connection info");
        });

        private static string GetFilePath(string filename)
        {
#if NET45
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
#elif NETSTANDARD1_3
            var baseDirectory = AppContext.BaseDirectory;
#endif
            // added because of test run issues on MacOS
            var indexOfBin = baseDirectory.LastIndexOf("bin", StringComparison.OrdinalIgnoreCase);
            var connectionStringFileDirectory = baseDirectory.Substring(0, (indexOfBin > 0) ? indexOfBin : baseDirectory.Length);
            return Path.Combine(connectionStringFileDirectory, filename);
        }
    }
}
