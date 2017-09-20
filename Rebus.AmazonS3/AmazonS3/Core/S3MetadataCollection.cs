using System;
using System.Collections.Generic;
using Amazon.S3.Model;

namespace Rebus.AmazonS3.Core
{
    internal class S3MetadataCollection
    {
        /// <summary>
        /// Amazon prefixes all user-defined metadata keys with this string
        /// </summary>
        internal const string UserDefinedMetadataPrefix = "x-amz-meta-";
        
        private readonly char _delimiter;
        private readonly KnownKeyEncoder _knownKeyEncoder;
        private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>();

        /// <summary>
        /// Create new empty metadata manager
        /// </summary>
        /// <param name="delimiter">Delimiter used to encode and decode metadata</param>
        /// <param name="knownKeyEncoder">Key encoder used for known keys</param>
        public S3MetadataCollection(char delimiter, KnownKeyEncoder knownKeyEncoder)
        {
            _delimiter = delimiter;
            _knownKeyEncoder = knownKeyEncoder;
        }

        /// <summary>
        /// Add or override range of metadata entries
        /// </summary>
        /// <param name="enumerable">Enumerable to add entries from</param>
        public void AddRange(IEnumerable<KeyValuePair<string, string>> enumerable)
        {
            if (enumerable == null) return;
            
            foreach (var keyValuePair in enumerable)
            {
                this[keyValuePair.Key] = keyValuePair.Value;
            }
        }

		/// <summary>
		/// Add or override range of metadata entries
		/// </summary>
		/// <remarks>
		/// Entries in the metadata collection are decoded
		/// </remarks>
		/// <param name="metadataCollection">Metadata collection to add entries from</param>
		public void LoadFrom(MetadataCollection metadataCollection)
		{
			if (metadataCollection == null) throw new ArgumentNullException(nameof(metadataCollection));

			foreach (var metadataKey in metadataCollection.Keys)
			{
				if (!metadataKey.StartsWith(UserDefinedMetadataPrefix, StringComparison.Ordinal)) continue;

				if (Decode(metadataKey, metadataCollection[metadataKey], out var key, out var value))
				{
					_metadata[key] = value;
				}
			}
		}

        /// <summary>
        /// Save all metadata to MetadataCollection
        /// </summary>
        /// <remarks>
        /// Entries are encoded before being added to the metadata collection
        /// </remarks>
        /// <param name="metadataCollection"></param>
        public void SaveTo(MetadataCollection metadataCollection)
        {
            if (metadataCollection == null) throw new ArgumentNullException(nameof(metadataCollection));

            var encoder = CreateEncoder();
            foreach (var entry in _metadata)
            {
                var (key, value) = encoder(entry.Key, entry.Value);
                metadataCollection[key] = value;
            }
        }

        /// <summary>
        /// Add or override metadata entry
        /// </summary>
        /// <param name="key">Key</param>
        public string this[string key]
        {
            get => _metadata.TryGetValue(key, out var value) ? value : null;
            set => _metadata[key] = value;
        }

        /// <summary>
        /// Return dictionary of keys and values stored in collection
        /// </summary>
        /// <value>As dictionary.</value>
        public Dictionary<string, string> ToDictionary() 
        {
            return new Dictionary<string, string>(_metadata);
        }

        /// <summary>
        /// Number of key/value pairs in collection
        /// </summary>
        /// <value>The count.</value>
        public int Count => _metadata.Count;

        private Func<string,string,(string Key, string Value)> CreateEncoder()
        {
            var counter = 0;
            
            return (key, value) =>
            {
                // Check if key is known
                if (_knownKeyEncoder.TryEncode(key, out var encodedKey))
                {
                    return (encodedKey, value);
                }
                
                // Otherwise use regular delimiter encoding scheme
                if (key.IndexOf(_delimiter) >= 0)
                    throw new ArgumentException($"Metadata key must not contain delimiter {_delimiter}", nameof(key));

                return (counter++.ToString(), $"{key}{_delimiter}{value}");
            };
        }

        private bool Decode(string encodedKey, string encodedValue, out string key, out string value)
        {
            key = value = null;
            
            if (encodedValue == null) 
                return false;

            if (!encodedKey.StartsWith(UserDefinedMetadataPrefix, StringComparison.Ordinal))
                return false;
            
            // Check if key is known
            var userKeyPart = encodedKey.Substring(UserDefinedMetadataPrefix.Length);
            if (_knownKeyEncoder.TryDecode(userKeyPart, out key))
            {
                value = encodedValue;
                return true;
            }

            // Try decode key/value from value
            var index = encodedValue.IndexOf(_delimiter);
            if (index < 0)
            {
                return false;
            }

            key = encodedValue.Substring(0, index);
            value = encodedValue.Substring(index + 1);
            return true;
        }
    }
}