using System.Collections.Generic;
using System.Linq;
using Amazon.S3.Model;
using NUnit.Framework;
using Rebus.AmazonS3.Core;

namespace Rebus.AmazonS3.Tests.Core;

[TestFixture]
public class S3MetadataManagerTest
{
    private const char Delimiter = '=';
    private const string KnownKey = "This Is A Known Key";
    private const string KnownKeyEncoded = "this-is-a-known-key";
    private S3MetadataCollection _metadataCollection;

    [SetUp]
    public void Setup()
    {
        var knownKeyEncoder = new KnownKeyEncoder(new HashSet<string> { KnownKey });
        _metadataCollection = new S3MetadataCollection(Delimiter, knownKeyEncoder);
    }

    [Test]
    public void Is_Initially_Empty()
    {
        Assert.AreEqual(0, _metadataCollection.Count);
    }

    [Test]
    public void Decodes_Keys_And_Values_From_MetadataCollection()
    {
        // Setup
        const string key = "Some Key";
        const string value = "Some Value";
        var metadata = CreateBuilder()
            .AddIndexed(key, value)
            .Build();

        // Test
        _metadataCollection.LoadFrom(metadata);

        // Verify
        var dict = _metadataCollection.ToDictionary();
        Assert.AreEqual(1, dict.Count);

        Assert.IsTrue(dict.TryGetValue(key, out var retrievedValue));
        Assert.AreEqual(value, retrievedValue);
    }

    [Test]
    public void Decodes_Known_Keys_And_Values_From_MetadataCollection()
    {
        // Setup
        const string value = "Some Value";
        var metadata = CreateBuilder()
            .AddKnown(KnownKeyEncoded, value)
            .Build();

        // Test
        _metadataCollection.LoadFrom(metadata);

        // Verify
        var dict = _metadataCollection.ToDictionary();
        Assert.AreEqual(1, dict.Count);

        Assert.IsTrue(dict.TryGetValue(KnownKey, out var retrievedValue));
        Assert.AreEqual(value, retrievedValue);
    }

    [Test]
    public void Decodes_Both_Known_And_Indexed_Keys_And_Values_From_MetadataCollection()
    {
        // Setup
        const string indexedKey = "Indexed Key";
        const string indexedValue = "Indexed Value";
        const string knownValue = "Known Value";
        var metadata = CreateBuilder()
            .AddIndexed(indexedKey, indexedValue)
            .AddKnown(KnownKeyEncoded, knownValue)
            .Build();

        // Test
        _metadataCollection.LoadFrom(metadata);

        // Verify
        var dict = _metadataCollection.ToDictionary();
        Assert.AreEqual(2, dict.Count);

        Assert.IsTrue(dict.TryGetValue(indexedKey, out var retrievedIndexedValue));
        Assert.AreEqual(indexedValue, retrievedIndexedValue);

        Assert.IsTrue(dict.TryGetValue(KnownKey, out var retrievedKnownValue));
        Assert.AreEqual(knownValue, retrievedKnownValue);
    }

    [Test]
    public void Encodes_Keys_And_Values_To_MetadataCollection() 
    {
        // Setup
        const string key = "Some Key";
        const string value = "Some Valye";
        _metadataCollection[key] = value;

        // Test
        var metadata = new MetadataCollection();
        _metadataCollection.SaveTo(metadata);

        // Verify
        Assert.AreEqual(1, metadata.Count);

        var indexedKey = metadata.Keys.First();
        Assert.AreEqual($"{key}{Delimiter}{value}", metadata[indexedKey]);
    }

    [Test]
    public void Encodes_Known_Keys_And_Values_To_MetadataCollection()
    {
        // Setup
        const string knownValue = "Known Value";
        _metadataCollection[KnownKey] = knownValue;

        // Test
        var metadata = new MetadataCollection();
        _metadataCollection.SaveTo(metadata);

        // Verify
        Assert.AreEqual(1, metadata.Count);

        var key = $"{S3MetadataCollection.UserDefinedMetadataPrefix}{KnownKeyEncoded}";
        Assert.IsTrue(metadata.Keys.Contains(key));
        Assert.AreEqual(knownValue, metadata[key]);
    }

    private MetadataCollectionBuilder CreateBuilder()
    {
        return new MetadataCollectionBuilder(Delimiter);
    }
}

internal class MetadataCollectionBuilder
{
    private readonly MetadataCollection _metadata = new MetadataCollection();
    private readonly char _delimiter;
    private int _index = 0;

    public MetadataCollectionBuilder(char delimiter)
    {
        _delimiter = delimiter;
    }

    public MetadataCollectionBuilder AddIndexed(string key, string value)
    {
        _metadata[$"{S3MetadataCollection.UserDefinedMetadataPrefix}{_index++}"] = $"{key}{_delimiter}{value}";
        return this;
    }

    public MetadataCollectionBuilder AddKnown(string encodedKey, string value)
    {
        _metadata[$"{S3MetadataCollection.UserDefinedMetadataPrefix}{encodedKey}"] = value;
        return this;
    }

    public MetadataCollection Build()
    {
        return _metadata;
    }
}