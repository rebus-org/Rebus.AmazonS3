using System.Collections.Generic;
using Rebus.Config;
using Rebus.DataBus;

namespace Rebus.AmazonS3.Core
{
    internal class S3MetadataCollectionFactory
    {
        private readonly char _delimiter;
        private readonly KnownKeyEncoder _knownKeyEncoder;
        
        public S3MetadataCollectionFactory(AmazonS3DataBusOptions options)
        {
            _delimiter = options.MetadataDelimiter;

            var knownKeys = new HashSet<string> {
                MetadataKeys.ContentEncoding,
                MetadataKeys.Length,
                MetadataKeys.ReadTime,
                MetadataKeys.SaveTime
            };

            if (options.KnownMetadataKeys != null) {
                foreach (var key in options.KnownMetadataKeys) {
                    knownKeys.Add(key);
                }
            }

            _knownKeyEncoder = new KnownKeyEncoder(knownKeys);
        }

        public S3MetadataCollection Create()
        {
            return new S3MetadataCollection(_delimiter, _knownKeyEncoder);
        }
    }
}