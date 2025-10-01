using System;
using System.Activities;
using System.Collections.Generic;

namespace TestCases.Workflows.WF4Samples;

public class ImproveAssignabilityOutArgumentActivity : NativeActivity
{
    // OutArgument of type List<string>
    public OutArgument<List<string>> Result { get; set; }

    // Additional OutArguments
    public OutArgument<DateTime> DateTimeOutArgument { get; set; }
    public OutArgument<DateTime?> NullableDateTimeOutArgument { get; set; }

    public ImproveAssignabilityOutArgumentActivity() : base()
    {
    }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        // Result
        var resultArg = new RuntimeArgument(
            "Result",
            typeof(List<string>),
            ArgumentDirection.Out);
        metadata.Bind(this.Result, resultArg);
        metadata.AddArgument(resultArg);

        // DateTimeOutArgument
        var dateTimeArg = new RuntimeArgument(
            "DateTimeOutArgument",
            typeof(DateTime),
            ArgumentDirection.Out);
        metadata.Bind(this.DateTimeOutArgument, dateTimeArg);
        metadata.AddArgument(dateTimeArg);

        // NullableDateTimeOutArgument
        var nullableDateTimeArg = new RuntimeArgument(
            "NullableDateTimeOutArgument",
            typeof(DateTime?),
            ArgumentDirection.Out);
        metadata.Bind(this.NullableDateTimeOutArgument, nullableDateTimeArg);
        metadata.AddArgument(nullableDateTimeArg);
    }

    protected override void Execute(NativeActivityContext context)
    {
        var result = new List<string>
            {
                "aaa",
                "bbb",
                "ccc"
            };

        Result.Set(context, result);
        DateTimeOutArgument.Set(context, DateTime.Now);
        NullableDateTimeOutArgument.Set(context, DateTime.Now);
    }
}
