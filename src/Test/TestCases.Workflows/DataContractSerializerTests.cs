using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Xunit;

namespace TestCases.Workflows
{
    public class DataContractSerializerTests
    {
        [Fact]
        public void VerifyCompiledLocationSerialized()
        {
            //arrange
            var activityContext = new ActivityContext();
            var activity = new Sequence();
            activity.InitializeAsRoot(null);
            activityContext.Reinitialize(new ActivityInstance(activity), null, activity, 0);

            var compiledLocation = new CompiledLocation<string>(getMethod: null,
                setMethod: null,
                locationReferences: new List<LocationReference>() { new RuntimeArgument(nameof(VerifyCompiledLocationSerialized), typeof(string), ArgumentDirection.Out) },
                locations: null,
                expressionId: 0,
                compiledRootActivity: activity,
                currentActivityContext: activityContext);

            var dataContractSerializer = new DataContractSerializer(typeof(CompiledLocation<string>));
            using MemoryStream stream = new MemoryStream();

            //serialize
            dataContractSerializer.WriteObject(stream, compiledLocation);

            //deserialize
            stream.Position = 0;
            compiledLocation = (CompiledLocation<string>)dataContractSerializer.ReadObject(stream);

            //assert
            compiledLocation.locationReferenceCache.Count.ShouldBe(1);
            var itemSavedInCache = compiledLocation.locationReferenceCache.First();
            itemSavedInCache.Item1.ShouldBe(nameof(VerifyCompiledLocationSerialized));
            itemSavedInCache.Item2.ShouldBe(typeof(string).AssemblyQualifiedName);
        }
    }
}
