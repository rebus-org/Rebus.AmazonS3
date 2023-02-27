using NUnit.Framework;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.AmazonS3.Tests;

[TestFixture]
public class AmazonS3DataBusTest : GeneralDataBusStorageTests<AmazonS3DataBusStorageFactory>
{
}