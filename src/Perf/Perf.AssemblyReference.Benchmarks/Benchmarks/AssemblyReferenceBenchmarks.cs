using BenchmarkDotNet.Attributes;
using Polly;
using Serilog;
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
        const int lookupCount = 1000;
        int[] lookupArray = new int[lookupCount];
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
            SixLabors.ImageSharp.Image.Load("C:\\Users\\marius.bughiu\\Downloads\\IMG_6914.jpg");
            var validator = new FluentValidation.InlineValidator<object>();
            var policy = Polly.Policy.Handle<Exception>().Retry(1);
            var date = NodaTime.SystemClock.Instance.GetCurrentInstant();
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

        //[Benchmark]
        //public void SequentialLookup_V2()
        //{
        //    for (int i = 0; i < lookupCount; i++)
        //    {
        //        AssemblyReferenceV2.GetAssembly(lookupAssemblies[i % lookupAssemblies.Length]);
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
    }
}
