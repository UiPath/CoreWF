using System.Xml.Linq;
using TestObjects.Xaml.GraphCore;

namespace TestCases.Xaml.Common.XamlOM
{
    public abstract class GraphNodeXaml : GraphNode, IXamlWritable
    {
        public static readonly string Xaml2006Ns = Constants.Namespace2006;
        public static readonly string Xaml2008Ns = Constants.NamespaceV2;
        public static readonly string XaslNs = Constants.NamespaceBuiltinTypes;

        protected const string BeforeWriteTraceProp = "BeforeWrite";
        protected const string AfterWriteTraceProp = "AfterWrite";

        public abstract void WriteBegin(IXamlWriter writer);
        public abstract void WriteEnd(IXamlWriter writer);

        public static object BeforeWriteTrace(ITestDependencyObject props)
        {
            return props.GetValue(BeforeWriteTraceProp);

        }

        public static object AfterWriteTrace(ITestDependencyObject props)
        {
            return props.GetValue(AfterWriteTraceProp);
        }

        public static XName BuildXName(string ns, string localName)
        {
            return string.Format("{{{0}}}{1}", ns, localName);
        }
    }
}
