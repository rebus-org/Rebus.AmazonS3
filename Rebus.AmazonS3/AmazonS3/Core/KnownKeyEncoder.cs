using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rebus.AmazonS3.Core
{
    internal class KnownKeyEncoder
    {
        private static readonly Regex IllegalCharacters = new Regex(@"[^a-z]");
        private const string Replacement = "-";

        private readonly Dictionary<string, string> _forwardLookup;
        private readonly Dictionary<string, string> _reverseLookup; 
        
        public KnownKeyEncoder(ISet<string> knownKeys)
        {
            if (knownKeys == null) throw new ArgumentNullException(nameof(knownKeys));
            
            _forwardLookup = new Dictionary<string, string>(knownKeys.Count);
            _reverseLookup = new Dictionary<string, string>(knownKeys.Count);

            foreach (var knownKey in knownKeys)
            {
                var encodedKey = Encode(knownKey);

                if (_reverseLookup.ContainsKey(encodedKey))
                {
                    throw new ArgumentException($"Set of known keys, contains two keys that map to the same encoded key (example '{knownKey}' => '{encodedKey}')");
                }

                _forwardLookup[knownKey] = encodedKey;
                _reverseLookup[encodedKey] = knownKey;
            }
        }

        public bool TryEncode(string key, out string encodedKey)
        {
            return _forwardLookup.TryGetValue(key, out encodedKey);
        }

        public bool TryDecode(string encodedKey, out string key)
        {
            return _reverseLookup.TryGetValue(encodedKey, out key);
        }
        
        private static string Encode(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            var encodedKey = key.ToLowerInvariant();
            encodedKey = IllegalCharacters.Replace(encodedKey, Replacement);

            return encodedKey;
        }
    }
}