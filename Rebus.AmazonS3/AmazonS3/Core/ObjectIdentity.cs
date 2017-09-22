using System.Text;
using Rebus.Config;

namespace Rebus.AmazonS3.Core
{
    internal class ObjectIdentity
    {
        public ObjectIdentity(string id, AmazonS3DataBusOptions options)
        {
            Id = id;
            Key = CreateObjectKey(id, options.ObjectKeyPrefix, options.ObjectKeySuffix);
        }

        public string Id { get; }
        public string Key { get; }

        private static string CreateObjectKey(string id, string prefix, string suffix)
        {
            var keyBuilder = new StringBuilder();
            if (prefix != null)
            {
                keyBuilder.Append(prefix);
            }

            keyBuilder.Append(id);

            if (suffix != null)
            {
                keyBuilder.Append(suffix);
            }

            return keyBuilder.ToString();
        }
    }
}