﻿using System;
using System.IO;
using Amazon.S3.Transfer;
using Rebus.DataBus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.AmazonS3.Tests
{
    public class AmazonS3DataBusStorageFactory : IDataBusStorageFactory
    {
        private const string ConnectionInfoFile = "s3_connectioninfo.txt";
        private const string ConnectionInfoEnvironment = "REBUS_S3_CONNECTIONINFO";
        
        private static readonly Lazy<ConnectionInfo> ConnectionInfo = new Lazy<ConnectionInfo>(() =>
        {
            // Load from file
            var file = GetFilePath(ConnectionInfoFile);
            if (File.Exists(file))
            {
                return Tests.ConnectionInfo.CreateFromString(File.ReadAllText(file));
            }
            
            // Load from environment
            var env = Environment.GetEnvironmentVariable(ConnectionInfoEnvironment);
            if (env != null)
            {
                return Tests.ConnectionInfo.CreateFromString(env);
            }

            throw new RebusConfigurationException("Missing Amazon S3 connection info");
        });
        
        public IDataBusStorage Create()
        {
            var connectionInfo = ConnectionInfo.Value;
            
            return new AmazonS3DataBusStorage(
                connectionInfo.Credentials, 
                connectionInfo.Config,
                connectionInfo.Options, 
                new TransferUtilityConfig(), 
                new ConsoleLoggerFactory(false));
        }

        public void CleanUp()
        {
        }

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