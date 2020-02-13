using System.IO;
using TestCases.Xaml.Common.XamlOM;

namespace TestCases.Xaml.Driver.XamlReaderWriter
{
    public interface IXamlReaderWriterTarget
    {
        XamlDocument Document { get; set; }
        bool ValidatingReader { get; set; }
        IXamlReader GetReader(Stream input);
        IXamlWriter GetWriter(Stream output);
    }

    public class XamlReaderWriterTarget<Reader, Writer> : IXamlReaderWriterTarget
        where Reader : IXamlReader, new()
        where Writer : IXamlWriter, new()
    {

        public Reader GetReader(Stream input)
        {
            Reader reader = new Reader();
            reader.Init(input);
            return reader;
        }

        public Writer GetWriter(Stream output)
        {
            Writer writer = new Writer();
            writer.Init(output);
            return writer;
        }

        #region IXamlReaderWriterTarget Members

        public XamlDocument Document { get; set; }

        public bool ValidatingReader { get; set; }

        IXamlReader IXamlReaderWriterTarget.GetReader(Stream input)
        {
            return GetReader(input);
        }

        IXamlWriter IXamlReaderWriterTarget.GetWriter(Stream output)
        {
            return GetWriter(output);
        }

        #endregion
    }
}
