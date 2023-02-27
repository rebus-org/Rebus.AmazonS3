using System;
using Amazon.S3.Transfer;
using Rebus.DataBus;
using Rebus.Logging;
using Rebus.Tests.Contracts.DataBus;
using Rebus.Time;

namespace Rebus.AmazonS3.Tests;

public class AmazonS3DataBusStorageFactory : IDataBusStorageFactory
{
    readonly FakeRebusTime _fakeRebusTime = new FakeRebusTime();

    public IDataBusStorage Create()
    {
        var connectionInfo = AmazonS3ConnectionInfoUtil.ConnectionInfo.Value;

        return new AmazonS3DataBusStorage(
            connectionInfo.Credentials,
            connectionInfo.Config,
            connectionInfo.Options,
            new TransferUtilityConfig(),
            new ConsoleLoggerFactory(false),
            _fakeRebusTime);
    }

    public void CleanUp()
    {
    }

    public void FakeIt(DateTimeOffset fakeTime) => _fakeRebusTime.Set(fakeTime);

    class FakeRebusTime : IRebusTime
    {
        DateTimeOffset? fakeNow;

        public DateTimeOffset Now => fakeNow ?? DateTimeOffset.Now;

        public void Set(DateTimeOffset newNow) => fakeNow = newNow;
    }
}