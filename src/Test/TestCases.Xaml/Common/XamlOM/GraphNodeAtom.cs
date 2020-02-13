using System;
using System.Xml;
using TestObjects.Xaml.GraphCore;

namespace TestCases.Xaml.Common.XamlOM
{
    public class GraphNodeAtom : GraphNodeXaml
    {
        public GraphNodeAtom()
        {
            Properties.Add(GraphNodeXaml.BeforeWriteTraceProp,
                new Func<string>(() => GetValue()));
        }

        string GetValue()
        {
            string value;

            if (GetDoNotExpectProp(this))
                return null;

            if (!String.IsNullOrEmpty(GetReaderRepresentation(this)))
            {
                value = GetReaderRepresentation(this);
            }
            else if (Value is XmlReader)
            {
                value = "XmlReader";
            }
            else
            {
                value = Value == null ? "null" : Value.ToString();
            }

            return "Atom:" + value;

        }
        public object Value { get; set; }
        public override void WriteBegin(IXamlWriter writer)
        {
            writer.WriteAtom(Value, this);
        }

        public override void WriteEnd(IXamlWriter writer)
        {
        }

        public const string GetReaderRepresentationProp = "GetReaderRepresentation";

        public static string GetReaderRepresentation(ITestDependencyObject props)
        {
            return (string)props.GetValue(GetReaderRepresentationProp);
        }

        public const string DoNotExpectProp = "DoNotExpect";

        public static bool GetDoNotExpectProp(ITestDependencyObject props)
        {
            object value = props.GetValue(DoNotExpectProp);
            if (value == null)
                return false;
            else
                return (bool)value;
        }
    }
}
