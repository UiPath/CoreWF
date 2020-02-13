using System.Xml.Linq;
using TestCases.Xaml.Common.XamlOM;
using TestObjects.Xaml.GraphCore;

namespace TestCases.Xaml.Driver.XamlReaderWriter
{
    public abstract class DecoratorXamlWritable : GraphNode, IXamlWritable
    {
        public abstract void WriteBegin(IXamlWriter writer);
        public abstract void WriteEnd(IXamlWriter writer);

        public override void SetValue(XName p, object value)
        {
            InnerWritable.SetValue(p, value);
        }

        public override object GetValue(XName p)
        {
            return InnerWritable.GetValue(p);
        }

        public virtual IXamlWritable InnerWritable { get; set; }

    }
}
