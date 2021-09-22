using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
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
                locationReferences: new List<LocationReference>() { new RuntimeArgument("name", typeof(string), ArgumentDirection.Out) },
                locations: null,
                expressionId: 0,
                compiledRootActivity: activity,
                currentActivityContext: activityContext);
            var dataContractSerializer = new DataContractSerializer(typeof(CompiledLocation<string>));
            using var stream = new MemoryStream();
            //act
            dataContractSerializer.WriteObject(stream, compiledLocation);
            stream.Position = 0;
            compiledLocation = (CompiledLocation<string>)dataContractSerializer.ReadObject(stream);
            //assert
            compiledLocation.LocationReferenceCache.Count.ShouldBe(1);
            (string name, string typeName) = compiledLocation.LocationReferenceCache[0];
            name.ShouldBe("name");
            typeName.ShouldBe(typeof(string).AssemblyQualifiedName);
        }
    }
}