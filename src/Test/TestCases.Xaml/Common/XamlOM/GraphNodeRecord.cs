using System;
using System.Collections.Generic;
using System.Xaml;
using System.Xml.Linq;

namespace TestCases.Xaml.Common.XamlOM
{
    public class GraphNodeRecord : GraphNodeXaml
    {
        public GraphNodeRecord()
        {
            Properties.Add(GraphNodeXaml.BeforeWriteTraceProp,
                new Func<string>(() => XamlNodeType.StartObject.ToString() + ":" + RecordName));
            Properties.Add(GraphNodeXaml.AfterWriteTraceProp,
                new Func<string>(() => XamlNodeType.EndObject.ToString() + ":" + RecordName));
        }

        public XName RecordName { get; set; }

        HashSet<XNamespace> expectedNamespaces = new HashSet<XNamespace>();
        public ICollection<XNamespace> ExpectedNamespaces
        {
            get
            {
                return expectedNamespaces;
            }
        }

        public bool IsObjectFromMember { get; set; }

        public override void WriteBegin(IXamlWriter writer)
        {
            writer.WriteStartRecord(RecordName, this);
        }

        public override void WriteEnd(IXamlWriter writer)
        {
            writer.WriteEndRecord(this);
        }
    }
}
