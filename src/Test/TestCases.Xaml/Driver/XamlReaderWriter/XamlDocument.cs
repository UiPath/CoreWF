using System;
using System.Collections.Generic;
using System.Text;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Xaml.Common.XamlOM;
using TestObjects.Xaml.GraphCore;

namespace TestCases.Xaml.Driver.XamlReaderWriter
{
    public class XamlDocument
    {
        public IXamlWritable Root { get; set; }

        ExpectedTrace trace = new ExpectedTrace(new OrderedTraces());

        public ExpectedTrace ExpectedTrace
        {
            get
            {
                return trace;
            }
        }

        public string XamlString { get; set; }

        public void Save(IXamlWriter writer, bool traceResults)
        {
            IXamlWritable data = BuildDecoratedTree(traceResults);
            data.DepthFirstSearchOperation(
                (node, state) => ((IXamlWritable)node).WriteBegin((IXamlWriter)state),
                (node, state) => ((IXamlWritable)node).WriteEnd((IXamlWriter)state),
                writer);

            writer.Close();
        }

        public void Save(IXamlWriter writer)
        {
            Save(writer, true);
        }

        IXamlWritable BuildDecoratedTree(bool traceResults)
        {
            if (!traceResults)
            {
                return Root;
            }

            return (IXamlWritable)Root.Transform(
                (node, state) => BuildDecoratedNode(node, traceResults),
                null);
        }

        IGraphNode BuildDecoratedNode(IGraphNode node, bool traceResults)
        {
            IGraphNode newNode = node;
            if (traceResults)
            {
                newNode = new TraceXamlWritable(ExpectedTrace) { InnerWritable = (IXamlWritable)newNode };
            }

            return newNode;
        }


    }
}
