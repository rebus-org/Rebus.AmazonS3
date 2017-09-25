using System.Reflection;
using NUnitLite;
using Rebus.AmazonS3.Tests;

namespace Rebus.AmazonS3.TestsRunner
{
    class Program
    {
        static int Main(string[] args)
        {
            return new AutoRun(typeof(AmazonS3DataBusTest).GetTypeInfo().Assembly).Execute(args);
        }
    }
}
