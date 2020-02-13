using System;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Xaml.Common.XamlOM;

namespace TestCases.Xaml.Driver.XamlReaderWriter
{
    class TraceXamlWritable : DecoratorXamlWritable
    {
        ExpectedTrace trace;
        public TraceXamlWritable(ExpectedTrace trace)
        {
            this.trace = trace;
        }

        public override void WriteBegin(IXamlWriter writer)
        {
            InnerWritable.WriteBegin(writer);

            string beforeWriteTrace = GetString(GraphNodeXaml.BeforeWriteTrace(InnerWritable));
            if (!String.IsNullOrEmpty(beforeWriteTrace))
            {
                trace.Trace.Steps.Add(new UserTrace(beforeWriteTrace));
            }


        }

        public override void WriteEnd(IXamlWriter writer)
        {
            InnerWritable.WriteEnd(writer);

            string afterWriteTrace = GetString(GraphNodeXaml.AfterWriteTrace(InnerWritable));
            if (!String.IsNullOrEmpty(afterWriteTrace))
            {
                trace.Trace.Steps.Add(new UserTrace(afterWriteTrace));
            }
        }

        string GetString(object value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is string)
            {
                return (string)value;
            }
            else if (value is Func<string>)
            {
                return ((Func<string>)value)();
            }
            else
            {
                return null;
            }
        }
    }
}
