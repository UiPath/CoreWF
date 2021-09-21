using Shouldly;
using System;
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

            using MemoryStream stream = new MemoryStream();
            new Action(() => new DataContractSerializer(typeof(CompiledLocation<string>)).WriteObject(stream, compiledLocation)).ShouldNotThrow();
        }
    }
}
