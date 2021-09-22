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
            var rootActivity = new Sequence();
            rootActivity.InitializeAsRoot(null);
            var activityContext = new ActivityContext(new ActivityInstance(rootActivity), null);
            var compiledLocation = new CompiledLocation<string>(null, null, new List<LocationReference>() { new Variable<string>("name") }, null, 0, 
                rootActivity, activityContext);
            var dataContractSerializer = new DataContractSerializer(typeof(CompiledLocation<string>));
            using var stream = new MemoryStream();
            //act
            dataContractSerializer.WriteObject(stream, compiledLocation);
            stream.Position = 0;
            compiledLocation = (CompiledLocation<string>)dataContractSerializer.ReadObject(stream);
            //assert
            compiledLocation.LocationReferenceCache.ShouldBe(new[]{("name", typeof(string).AssemblyQualifiedName)});
        }
    }
}