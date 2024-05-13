﻿using Newtonsoft.Json;
using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using WorkflowApplicationTestExtensions.Persistence;
using Xunit;

namespace TestCases.Workflows
{
    public class CompiledLocationSerializationTests
    {
        [Theory]
        [InlineData(false)] // DataContract
        [InlineData(true)]  // NewtonsoftJson
        public void VerifyCompiledLocationSerialized(bool useJsonSerialization)
        {
            //arrange
            var rootActivity = new Sequence();
            rootActivity.InitializeAsRoot(null);
            var activityContext = new ActivityContext(new ActivityInstance(rootActivity), null);
            var compiledLocation = new CompiledLocation<string>(null, null, new List<LocationReference>() { new Variable<string>("name") }, null, 0, 
                rootActivity, activityContext);

            //act
            compiledLocation = SerializeAndDeserialize(compiledLocation, useJsonSerialization);

            //assert
            compiledLocation.LocationReferenceCache.ShouldBe(new[]{("name", typeof(string).AssemblyQualifiedName)});

            static CompiledLocation<string> SerializeAndDeserialize(CompiledLocation<string> compiledLocation, bool useJsonSerialization)
            {
                if (useJsonSerialization)
                {
                    var json = JsonConvert.SerializeObject(compiledLocation, JsonWorkflowSerializer.SerializerSettings());
                    return JsonConvert.DeserializeObject<CompiledLocation<string>>(json, JsonWorkflowSerializer.SerializerSettings());
                }
                else
                {
                    var dataContractSerializer = new DataContractSerializer(typeof(CompiledLocation<string>));
                    using var stream = new MemoryStream();
                    dataContractSerializer.WriteObject(stream, compiledLocation);
                    stream.Position = 0;
                    return (CompiledLocation<string>)dataContractSerializer.ReadObject(stream);
                }
            }
        }
    }
}
