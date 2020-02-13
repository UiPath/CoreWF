using System.IO;
using System.Xml.Linq;
using TestObjects.Xaml.GraphCore;

namespace TestCases.Xaml.Common.XamlOM
{
    public interface IXamlWriter
    {
        void Init(Stream output);
        void WriteNamespace(string xamlNamespace, string prefix);
        XName GetCurrentType();
        void WriteRaw(string data, ITestDependencyObject props);
        void WriteAtom(object value, ITestDependencyObject props);
        void WriteStartRecord(XName typeName, ITestDependencyObject props);
        void WriteEndRecord(ITestDependencyObject props);
        void WriteStartMember(string memberName, ITestDependencyObject props);
        void WriteStartMember(string memberName, XName typeName, ITestDependencyObject props);
        void WriteEndMember(ITestDependencyObject props);
        void Close();
    }

    public interface IXamlWritable : IGraphNode
    {
        void WriteBegin(IXamlWriter writer);
        void WriteEnd(IXamlWriter writer);
    }
}
