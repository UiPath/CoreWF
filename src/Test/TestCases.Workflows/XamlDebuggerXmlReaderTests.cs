using System.Activities.Debugger;
using System.Activities.XamlIntegration;
using System.IO;
using System.Text;
using System.Xaml;
using System.Xml;
using Shouldly;
using Xunit;

namespace TestCases.Workflows
{
    public class XamlDebuggerXmlReaderTests
    {
        [Fact]
        public void OnLoad_NoPropertiesShouldBeAttachedForSourceLocationWhereTheValueCanBeTypeConvertedToString()
        {
            using var inputStream = TestHelper.GetXamlStream(TestXamls.SimpleWorkflowWithArgsAndVar);
            using var streamReader = new StreamReader(inputStream);
            using var xamlDebuggerReader = new XamlDebuggerXmlReader(streamReader)
            {
                CollectNonActivitySourceLocation = true
            };
            using var activityBuilderReader = ActivityXamlServices.CreateBuilderReader(xamlDebuggerReader);
            var xamlObject = XamlServices.Load(activityBuilderReader);

            using var stringWriter = new Utf8StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings {Indent = true});
            using var xamlWriter = ActivityXamlServices.CreateBuilderWriter(new XamlXmlWriter(xmlWriter, new XamlSchemaContext()));
            // Without the fix, Save will throw:
            // System.InvalidOperationException: 'An attachable property named 'System.Activities.Debugger.XamlDebuggerXmlReader.StartLine' is attached to a property named 'System.Activities.OutArgument`1[System.String]'.
            // The property named 'System.Activities.OutArgument`1[System.String]' is either a string or can be type-converted to string; attaching on such properties are not supported.
            // For debugging, the property 'System.Activities.OutArgument`1[System.String]' contains an object 'To'.'
            XamlServices.Save(xamlWriter, xamlObject);
            var xamlString = stringWriter.ToString();
            xamlString.ShouldNotBeNullOrEmpty();
        }

        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
    }
}
