using Amazon.S3.Transfer;
using Rebus.DataBus;
using Rebus.Logging;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.AmazonS3.Tests
{
    public class AmazonS3DataBusStorageFactory : IDataBusStorageFactory
    {
        public IDataBusStorage Create()
        {
            var connectionInfo = AmazonS3ConnectionInfoUtil.ConnectionInfo.Value;

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
    }
}