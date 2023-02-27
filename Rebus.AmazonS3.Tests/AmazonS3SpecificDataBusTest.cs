using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.Exceptions;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.AmazonS3.Tests;

[TestFixture]
public class AmazonS3SpecificDataBusTest : FixtureBase
{
    private const string KnownId = "known id";
        
    private IDataBusStorageFactory _factory;
    private IDataBusStorage _storage;
        
    protected override void SetUp()
    {
        _factory = new AmazonS3DataBusStorageFactory();
        _storage = _factory.Create();
    }

    protected override void TearDown()
    {
        _factory.CleanUp();
    }

    [Test]
    public void Throws_When_Trying_To_Use_Metadata_Delimiter_In_Key_Part()
    {
        // Setup
        var defaultDelimiter = new AmazonS3DataBusOptions("bucket").MetadataDelimiter;
        var illegalMetadata = new Dictionary<string, string>
        {
            [$"Illegal{defaultDelimiter}Key"] = "Any Value"
        };

        // Test
        var exception = Assert.Throws<AggregateException>(() =>
        {
            using var source = new MemoryStream(Array.Empty<byte>());
                
            _storage.Save(KnownId, source, illegalMetadata).Wait();
        });
            
        // Verify
        var rebusException = exception.GetBaseException();
        Assert.That(rebusException, Is.TypeOf<RebusApplicationException>());
        Assert.That(rebusException.InnerException, Is.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task Metadata_Key_May_Contain_Delimiter()
    {
        // Setup
        var defaultDelimiter = new AmazonS3DataBusOptions("bucket").MetadataDelimiter;
        const string key = "SomeKey";
        var metadata = new Dictionary<string,string>
        {
            [key] = $"Legal{defaultDelimiter}Value"
        };
            
        // Test
        await using (var source = new MemoryStream(Array.Empty<byte>()))
        {
            await _storage.Save(KnownId, source, metadata);
        }
            
        // Verify
        var readMetadata = await _storage.ReadMetadata(KnownId);

        Assert.That(readMetadata[key], Is.EqualTo(metadata[key]));
    }
}