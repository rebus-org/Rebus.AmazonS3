using System;
using System.Collections.Generic;
using Rebus.DataBus;

namespace Rebus.Config
{
    /// <summary>
    /// Holds all of the exposed options which can be applied using the S3 data bus.
    /// </summary>
    public class AmazonS3DataBusOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Rebus.Config.AmazonS3DataBusOptions"/> class.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        public AmazonS3DataBusOptions(string bucketName)
        {
            if (string.IsNullOrWhiteSpace(bucketName)) throw new ArgumentException("Canont be null or empty", nameof(bucketName));

            BucketName = bucketName;
            ObjectKeyPrefix = null;
            ObjectKeySuffix = null;
            MetadataDelimiter = '=';
            KnownMetadataKeys = null;
            AutoCreateBucket = true;
        }

        /// <summary>
        /// Name of bucket used to store attachments
        /// </summary>
        public string BucketName { get; set; }

        /// <summary>
        /// Optional prefix for object keys
        /// </summary>
        public string ObjectKeyPrefix { get; set; }

        /// <summary>
        /// If true, the <see cref="MetadataKeys.ReadTime"/> metadata key will NOT be updated on each read operation. This will save some requests
        /// </summary>
        public bool DoNotUpdateLastReadTime { get; set; }

        /// <summary>
        /// Optional postfix for object keys
        /// </summary>
        public string ObjectKeySuffix { get; set; }

        /// <summary>
        /// Metadata delimiter used for storing metadata key-value pairs
        /// </summary>
        /// <remarks>
        /// The metadata delimiter will be a reserved character, which cannot be used in the key-portion 
        /// when adding metadata. The default value is '='
        /// </remarks>
        public char MetadataDelimiter { get; set; }

        /// <summary>
        /// Optional set of known metadata keys which should be stored as S3 metadata keys rather
        /// than being encoded.
        /// </summary>
        /// <remarks>
        /// If set, allows a set of known keys to be specifically stored using both the S3 metadata
        /// key and value, using the key part to store the Rebus metadata key (encoded to only contain
        /// lower-case and hyphens), instead of encoding both the Rebus metadata key and value in the
        /// S3 metadata value (using <see cref="MetadataDelimiter"/>).
        /// 
        /// This allows for easier retrieval of S3 metadata key/values as the S3 metadata key will be
        /// pre-determined. This can be usful for monitoring or similar activites.
        /// </remarks>
        /// <value>The known keys.</value>
        public ISet<string> KnownMetadataKeys { get; set; }

        /// <summary>
        /// Whether or not to automatically create the bucket, if it doesn't already exist (default true)
        /// </summary>
        public bool AutoCreateBucket { get; set; }
    }
}
