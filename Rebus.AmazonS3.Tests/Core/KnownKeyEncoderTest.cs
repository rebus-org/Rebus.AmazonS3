using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.AmazonS3.Core;

namespace Rebus.AmazonS3.Tests.Core
{
    [TestFixture]
    public class KnownKeyEncoderTest
    {
        private const string KnownKey = "This Is A Known Key";
        private KnownKeyEncoder _knownKeyEncoder;

        [SetUp]
        public void Setup()
        {
            _knownKeyEncoder = new KnownKeyEncoder(new HashSet<string> { KnownKey });
        }

        [Test]
        public void Will_Encode_And_Decode_Known_Key() 
        {
            Assert.IsTrue(_knownKeyEncoder.TryEncode(KnownKey, out var encodedKey));

            Assert.AreEqual("this-is-a-known-key", encodedKey);

            Assert.IsTrue(_knownKeyEncoder.TryDecode(encodedKey, out var decodedKey));

            Assert.AreEqual(KnownKey, decodedKey);
        }

        [Test]
        public void Will_Not_Encode_Unknown_Key()
        {
            Assert.IsFalse(_knownKeyEncoder.TryEncode("Not A Known Key", out var encodedKey));

            Assert.IsNull(encodedKey);
        }

        [Test]
        public void Will_Not_Decode_Uknown_Encoded_Key()
        {
            Assert.IsFalse(_knownKeyEncoder.TryDecode("not-a-known-key", out var decodedKey));

            Assert.IsNull(decodedKey);
        }

        [Test]
        public void Will_Not_Allow_Multiple_Known_Keys_That_Encode_To_The_Same_Key() 
        {
            Assert.Throws<ArgumentException>(() => 
            {
                var knownKeyEncoder = new KnownKeyEncoder(new HashSet<string> { "Same Key", "same key" });
            });
        }
    }
}
