using Azure.Storage.Blobs;
using BenchmarkDotNet.Attributes;
using ClosedXML.Excel;
using CsvHelper.Configuration;
using HtmlAgilityPack;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using NodaTime;
using Polly;
using Serilog;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Activities.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using AssemblyReferenceOG = System.Activities.Expressions.AssemblyReference;

namespace Perf.AssemblyReference.Benchmarks
{
    /// <summary>
    /// For benchmarking improvements to GetAssembly simply create a copy of AssemblyReference.cs,
    /// name it AssemblyReferenceV2 and make your improvements there. Then uncomment the _V2 benchmarks
    /// below and run the project.
    /// </summary>
    [MemoryDiagnoser(true)]
    public class AssemblyReferenceBenchmarks
    {
        const int lookupCount = 10000;
        int[] lookupArray = new int[lookupCount];
        AssemblyName nonExistentAssemblyName = new AssemblyName("NonExistent.Assembly");
        AssemblyName[] lookupAssemblies;
        ParallelOptions parallelOptions_4Cores = new ParallelOptions { MaxDegreeOfParallelism = 4 };
        ParallelOptions parallelOptions_16Cores = new ParallelOptions { MaxDegreeOfParallelism = 16 };

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Force loads a bunch of assemblies to simulate a realistic environment
            ForceLoadAssemblies();

            lookupAssemblies = AssemblyLoadContext.All.SelectMany(c => c.Assemblies)
                // Duplicate assemblies to simulate duplicate requests from different threads
                .SelectMany(a => Enumerable.Range(0, 4), (a, i) => a.GetName())
                .ToArray();
        }

        internal static void ForceLoadAssemblies()
        {
            // Instantiate types from different NuGet packages to trigger assembly loading
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { hello = "world" });
            var config = new Serilog.LoggerConfiguration().WriteTo.Console().CreateLogger();
            config.Information("Logging from Serilog");

            var mapper = new AutoMapper.MapperConfiguration(cfg => { }).CreateMapper();
            var faker = new Bogus.Faker();
            var csv = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
            var document = new HtmlAgilityPack.HtmlDocument();

            var sheet = new ClosedXML.Excel.XLWorkbook();
            using Image<Rgba32> image = new Image<Rgba32>(400, 400);
            var validator = new FluentValidation.InlineValidator<object>();
            var policy = Polly.Policy.Handle<Exception>().Retry(1);
            var date = NodaTime.SystemClock.Instance.GetCurrentInstant();

            _ = Policy.Handle<Exception>().Retry(1);
            _ = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
            _ = new HtmlDocument();
            _ = SystemClock.Instance.GetCurrentInstant();
            _ = new SmtpClient();
            _ = new XLWorkbook();
            _ = new BlobClient(new Uri("https://fake.blob.core.windows.net/test"), new Azure.AzureSasCredential("token"));
            _ = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true).Build();
            _ = new ServiceCollection().AddLogging().BuildServiceProvider();
            _ = new DefaultHttpContext();

            var deps = DependencyContext.Default;
            var assemblies = deps.RuntimeLibraries;

            foreach (var lib in assemblies)
            {
                foreach (var assemblyName in lib.GetDefaultAssemblyNames(deps))
                {
                    try
                    {
                        if (AppDomain.CurrentDomain.GetAssemblies().All(a => a.GetName().Name != assemblyName.Name))
                        {
                            Assembly.Load(assemblyName);
                        }
                    }
                    catch
                    {
                        // Skip failed loads
                    }
                }
            }
        }

        [Benchmark]
        public void SequentialLookup()
        {
            for (int i = 0; i < lookupCount; i++)
            {
                AssemblyReferenceOG.GetAssembly(lookupAssemblies[i % lookupAssemblies.Length]);
            }
        }

        [Benchmark]
        public void SequentialLookup_CacheMiss()
        {
            for (int i = 0; i < lookupCount; i++)
            {
                AssemblyReferenceOG.GetAssembly(nonExistentAssemblyName);
            }
        }

        [Benchmark]
        public void ParallelLookup4Cores()
        {
            Parallel.ForEach(lookupArray, parallelOptions_4Cores, (i, token) =>
            {
                AssemblyReferenceOG.GetAssembly(lookupAssemblies[i % lookupAssemblies.Length]);
            });
        }

        [Benchmark]
        public void ParallelLookup16Cores()
        {
            Parallel.ForEach(lookupArray, parallelOptions_16Cores, (i, token) =>
            {
                AssemblyReferenceOG.GetAssembly(lookupAssemblies[i % lookupAssemblies.Length]);
            });
        }

        [Benchmark]
        public void ParallelLookup16Cores_CacheMiss()
        {
            Parallel.ForEach(lookupArray, parallelOptions_16Cores, (i, token) =>
            {
                AssemblyReferenceOG.GetAssembly(nonExistentAssemblyName);
            });
        }

        //[Benchmark]
        //public void SequentialLookup_V2()
        //{
        //    for (int i = 0; i < lookupCount; i++)
        //    {
        //        AssemblyReferenceV2.GetAssembly(lookupAssemblies[i % lookupAssemblies.Length]);
        //    }
        //}

        //[Benchmark]
        //public void SequentialLookup_CacheMiss_V2()
        //{
        //    for (int i = 0; i < lookupCount; i++)
        //    {
        //        AssemblyReferenceV2.GetAssembly(nonExistentAssemblyName);
        //    }
        //}

        //[Benchmark]
        //public void ParallelLookup4Cores_V2()
        //{
        //    Parallel.ForEach(lookupArray, parallelOptions_4Cores, (i, token) =>
        //    {
        //        AssemblyReferenceV2.GetAssembly(lookupAssemblies[i % lookupAssemblies.Length]);
        //    });
        //}

        //[Benchmark]
        //public void ParallelLookup16Cores_V2()
        //{
        //    Parallel.ForEach(lookupArray, parallelOptions_16Cores, (i, token) =>
        //    {
        //        AssemblyReferenceV2.GetAssembly(lookupAssemblies[i % lookupAssemblies.Length]);
        //    });
        //}

        //[Benchmark]
        //public void ParallelLookup16Cores_CacheMiss_V2()
        //{
        //    Parallel.ForEach(lookupArray, parallelOptions_16Cores, (i, token) =>
        //    {
        //        AssemblyReferenceV2.GetAssembly(nonExistentAssemblyName);
        //    });
        //}
    }
}
